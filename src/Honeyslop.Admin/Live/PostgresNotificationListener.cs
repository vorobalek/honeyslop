using System.Globalization;
using Honeyslop.Data.Repositories;
using Npgsql;

namespace Honeyslop.Admin.Live;

internal sealed partial class PostgresNotificationListener(
    string connectionString,
    LiveUpdateHub hub,
    GenerationOutputStore outputStore,
    TimeProvider timeProvider,
    ILogger<PostgresNotificationListener> logger) : BackgroundService
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
                await using (var command = new NpgsqlCommand("listen honeyslop_entries; listen honeyslop_requests; listen honeyslop_outputs;", connection))
                    await command.ExecuteNonQueryAsync(stoppingToken);
                LogListening(logger);
                hub.PublishEverything();
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

    private void Dispatch(string channel, string payload)
    {
        if (channel == "honeyslop_requests")
            hub.Publish(LiveUpdateHub.Requests);
        else if (channel == "honeyslop_outputs")
        {
            if (GenerationOutput.FromPayload(payload) is not { } output)
                return;
            outputStore.Set(output);
            hub.Publish(LiveUpdateHub.Outputs, output.EntryId);
        }
        else if (long.TryParse(payload, NumberStyles.Integer, CultureInfo.InvariantCulture, out var entryId))
            hub.Publish(LiveUpdateHub.Entries, entryId);
        else
            hub.Publish(LiveUpdateHub.Entries);
    }

    [LoggerMessage(LogLevel.Information, "Listening for database changes")]
    static partial void LogListening(ILogger<PostgresNotificationListener> logger);

    [LoggerMessage(LogLevel.Warning, "Lost the database change feed, reconnecting")]
    static partial void LogDisconnected(ILogger<PostgresNotificationListener> logger, Exception exception);
}
