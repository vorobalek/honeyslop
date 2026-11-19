using System.Globalization;
using Honeyslop.Trap.Settings;
using Npgsql;

namespace Honeyslop.Trap.Caching.Internals;

internal sealed partial class ChangeListener(
    string connectionString,
    IResponseCache responseCache,
    ISettingsProvider settingsProvider,
    IClientDelays clientDelays,
    TimeProvider timeProvider,
    ILogger<ChangeListener> logger) : BackgroundService
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            KeepAlive = 30,
            Pooling = false
        };
        while (!stoppingToken.IsCancellationRequested)
            try
            {
                await using var connection = new NpgsqlConnection(builder.ConnectionString);
                connection.Notification += (_, notification) => Dispatch(notification.Channel, notification.Payload);
                await connection.OpenAsync(stoppingToken);
                await using (var command = new NpgsqlCommand("listen honeyslop_entries; listen honeyslop_settings; listen honeyslop_clients;", connection))
                {
                    await command.ExecuteNonQueryAsync(stoppingToken);
                }

                responseCache.Enable();
                settingsProvider.Reset();
                clientDelays.Reset();
                LogListening(logger);
                while (!stoppingToken.IsCancellationRequested)
                    await connection.WaitAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                responseCache.Disable();
                LogDisconnected(logger, exception);
                await Task.Delay(ReconnectDelay, timeProvider, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
            }
    }

    internal void Dispatch(string channel, string payload)
    {
        if (channel == "honeyslop_settings")
            settingsProvider.Reset();
        else if (channel == "honeyslop_clients")
            clientDelays.Reset();
        else if (long.TryParse(payload, NumberStyles.Integer, CultureInfo.InvariantCulture, out var entryId))
            responseCache.Evict(entryId);
        else
            responseCache.Clear();
    }

    [LoggerMessage(LogLevel.Information, "Listening for entry and settings changes")]
    static partial void LogListening(ILogger<ChangeListener> logger);

    [LoggerMessage(LogLevel.Warning, "Lost the change feed, serving without the memory cache until it is back")]
    static partial void LogDisconnected(ILogger<ChangeListener> logger, Exception exception);
}