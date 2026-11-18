using System.Globalization;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Common.Serializers;
using Honeyslop.Llm.Contracts;
using Npgsql;

namespace Honeyslop.Admin.Live;

internal sealed partial class PostgresNotificationListener(
    string connectionString,
    ILiveUpdateHub hub,
    IJsonSerializer<GenerationOutputDto> outputSerializer,
    IGenerationOutputStore outputStore,
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
                await using (var command = new NpgsqlCommand("listen honeyslop_entries; listen honeyslop_requests; listen honeyslop_outputs; listen honeyslop_reports;", connection))
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
            hub.Publish(LiveTopics.Requests);
        else if (channel == "honeyslop_reports")
            hub.Publish(LiveTopics.Reports);
        else if (channel == "honeyslop_outputs")
        {
            if (outputSerializer.Deserialize(payload) is not { } output)
                return;
            outputStore.Set(output);
            hub.Publish(LiveTopics.Outputs, output.EntryId);
        }
        else if (long.TryParse(payload, NumberStyles.Integer, CultureInfo.InvariantCulture, out var entryId))
            hub.Publish(LiveTopics.Entries, entryId);
        else
            hub.Publish(LiveTopics.Entries);
    }

    [LoggerMessage(LogLevel.Information, "Listening for database changes")]
    static partial void LogListening(ILogger<PostgresNotificationListener> logger);

    [LoggerMessage(LogLevel.Warning, "Lost the database change feed, reconnecting")]
    static partial void LogDisconnected(ILogger<PostgresNotificationListener> logger, Exception exception);
}
