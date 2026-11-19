using System.Globalization;
using Honeyslop.Admin.Core.Access;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Admin.Live.Contracts;
using Honeyslop.Admin.Live.Enums;
using Honeyslop.Common.Serializers;
using Honeyslop.Llm.Contracts;
using Npgsql;

namespace Honeyslop.Admin.Live;

internal sealed partial class PostgresNotificationListener(
    string connectionString,
    ILiveSubscriptions subscriptions,
    IJsonSerializer<GenerationOutputDto> outputSerializer,
    IGenerationOutputStore outputStore,
    IPublicView publicView,
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
                await using (var command = new NpgsqlCommand("listen honeyslop_entries; listen honeyslop_requests; listen honeyslop_outputs; listen honeyslop_reports; listen honeyslop_settings; listen honeyslop_clients;", connection))
                {
                    await command.ExecuteNonQueryAsync(stoppingToken);
                }

                LogListening(logger);
                subscriptions.PublishEverything();
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
        if (channel == "honeyslop_settings")
        {
            publicView.Reset();
        }
        else if (channel == "honeyslop_requests")
        {
            subscriptions.Publish(new LiveChange(LiveTopic.Requests));
        }
        else if (channel == "honeyslop_reports")
        {
            subscriptions.Publish(new LiveChange(LiveTopic.Reports));
        }
        else if (channel == "honeyslop_clients")
        {
            subscriptions.Publish(new LiveChange(LiveTopic.Clients));
        }
        else if (channel == "honeyslop_outputs")
        {
            if (outputSerializer.Deserialize(payload) is not { } output)
                return;
            outputStore.Set(output);
            subscriptions.Publish(new LiveChange(LiveTopic.Outputs, output.EntryId));
        }
        else if (long.TryParse(payload, NumberStyles.Integer, CultureInfo.InvariantCulture, out var entryId))
        {
            subscriptions.Publish(new LiveChange(LiveTopic.Entries, entryId));
        }
        else
        {
            subscriptions.Publish(new LiveChange(LiveTopic.Entries));
        }
    }

    [LoggerMessage(LogLevel.Information, "Listening for database changes")]
    static partial void LogListening(ILogger<PostgresNotificationListener> logger);

    [LoggerMessage(LogLevel.Warning, "Lost the database change feed, reconnecting")]
    static partial void LogDisconnected(ILogger<PostgresNotificationListener> logger, Exception exception);
}