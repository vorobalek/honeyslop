using System.Net;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Providers;
using Honeyslop.Data.Repositories;
using Honeyslop.Data.Requests;
using Honeyslop.Trap.Monitoring;
using Honeyslop.Trap.Settings;

namespace Honeyslop.Trap.Requests;

internal sealed partial class RequestLogWriter(
    IRequestLog requestLog,
    IServiceScopeFactory serviceScopeFactory,
    ISettingsProvider settingsProvider,
    ILogger<RequestLogWriter> logger) : BackgroundService
{
    private const int BatchSize = 500;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var first in requestLog.ReadAllAsync(stoppingToken))
                await WriteAsync(Batch(first), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var remaining = new List<RequestLogItem>();
        while (requestLog.TryRead(out var item))
            remaining.Add(item);
        if (remaining.Count > 0)
            await WriteAsync(remaining, shutdown.Token);
    }

    private List<RequestLogItem> Batch(RequestLogItem first)
    {
        var batch = new List<RequestLogItem>
        {
            first
        };
        while (batch.Count < BatchSize && requestLog.TryRead(out var item))
            batch.Add(item);
        return batch;
    }

    private async Task WriteAsync(List<RequestLogItem> batch, CancellationToken cancellationToken)
    {
        var inserts = batch
            .Where(x => !x.IsUpdate)
            .Select(x => x.Record)
            .Distinct(ReferenceEqualityComparer.Instance)
            .Cast<RequestRecord>()
            .ToArray();
        try
        {
            await using var asyncScope = serviceScopeFactory.CreateAsyncScope();
            var repository = asyncScope.ServiceProvider.GetRequiredService<IRequestRepository>();
            if (inserts.Length > 0)
                await repository.AddRangeAsync(inserts, cancellationToken);

            var updates = batch
                .Where(x => x.IsUpdate)
                .Select(x => x.Record)
                .Distinct(ReferenceEqualityComparer.Instance)
                .Cast<RequestRecord>()
                .ToArray();
            var lost = updates.Where(x => x.Id == 0).ToArray();
            if (lost.Length > 0)
                await repository.AddRangeAsync(lost, cancellationToken);
            var stored = updates.Where(x => x.Id != 0).ToArray();
            if (stored.Length > 0)
                await repository.UpdateRangeAsync(stored, cancellationToken);
            TrapMetrics.RequestLogWritten.Inc(inserts.Length + lost.Length);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            TrapMetrics.RequestLogDropped.Inc(batch.Count);
            LogWriteFailed(logger, exception, batch.Count);
            return;
        }

        try
        {
            if (!(await settingsProvider.GetAsync(cancellationToken)).AutoDelayEnabled)
                return;
            var clients = Clients(batch);
            if (clients.Length == 0)
                return;
            await using var asyncScope = serviceScopeFactory.CreateAsyncScope();
            var now = await asyncScope.ServiceProvider.GetRequiredService<IServerTimeRepository>().GetNowAsync(cancellationToken);
            await asyncScope.ServiceProvider.GetRequiredService<IDelayLearningJobRepository>().EnqueueAsync(clients, now, cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogEnqueueFailed(logger, exception);
        }
    }

    private static string[] Clients(List<RequestLogItem> batch)
    {
        return
        [
            .. batch
                .Select(x => x.Record)
                .Where(x => x.Outcome is RequestOutcome.Hit or RequestOutcome.Generated or RequestOutcome.Abandoned)
                .Select(x => IPAddress.TryParse(x.ClientIp, out var address) ? ClientAddress.Key(address) : null)
                .OfType<string>()
                .Distinct(StringComparer.Ordinal)
        ];
    }

    [LoggerMessage(LogLevel.Warning, "Failed to store {count} request records")]
    static partial void LogWriteFailed(ILogger<RequestLogWriter> logger, Exception exception, int count);

    [LoggerMessage(LogLevel.Warning, "Failed to queue clients for delay learning")]
    static partial void LogEnqueueFailed(ILogger<RequestLogWriter> logger, Exception exception);
}