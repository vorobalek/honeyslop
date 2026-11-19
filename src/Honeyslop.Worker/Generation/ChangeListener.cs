using Honeyslop.Data.Providers;
using Npgsql;

namespace Honeyslop.Worker.Generation;

internal sealed partial class ChangeListener(
    string connectionString,
    IGenerationSignal generationSignal,
    ISettingsProvider settingsProvider,
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
                connection.Notification += (_, notification) => Dispatch(notification.Channel);
                await connection.OpenAsync(stoppingToken);
                await using (var command = new NpgsqlCommand("listen honeyslop_entries; listen honeyslop_settings;", connection))
                {
                    await command.ExecuteNonQueryAsync(stoppingToken);
                }

                settingsProvider.Reset();
                generationSignal.Notify();
                LogListening(logger);
                while (!stoppingToken.IsCancellationRequested)
                    await connection.WaitAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                LogDisconnected(logger, exception);
                await Task.Delay(ReconnectDelay, timeProvider, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
            }
    }

    internal void Dispatch(string channel)
    {
        if (channel == "honeyslop_settings")
            settingsProvider.Reset();
        else
            generationSignal.Notify();
    }

    [LoggerMessage(LogLevel.Information, "Listening for entry and settings changes")]
    static partial void LogListening(ILogger<ChangeListener> logger);

    [LoggerMessage(LogLevel.Warning, "Lost the database notifications, reconnecting")]
    static partial void LogDisconnected(ILogger<ChangeListener> logger, Exception exception);
}