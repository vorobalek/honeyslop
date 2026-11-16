using Honeyslop.Data.Repositories;
using Honeyslop.Trap.Options;
using Microsoft.Extensions.Options;

namespace Honeyslop.Trap.Hits;

internal sealed partial class HitFlushService(
    IHitCounter hitCounter,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<TrapOptions> options,
    ILogger<HitFlushService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.HitFlushInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await FlushAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await FlushAsync(shutdown.Token);
    }

    private async Task FlushAsync(CancellationToken cancellationToken)
    {
        var batches = hitCounter.Drain();
        if (batches.Count == 0)
            return;

        try
        {
            await using var asyncScope = serviceScopeFactory.CreateAsyncScope();
            var entryRepository = asyncScope.ServiceProvider.GetRequiredService<IEntryRepository>();
            foreach (var batch in batches)
                await entryRepository.AddHitsAsync(
                    batch.EntryId,
                    batch.Count,
                    batch.LastHitAt,
                    cancellationToken);
        }
        catch (Exception exception)
        {
            LogFlushFailed(logger, exception, batches.Count);
        }
    }

    [LoggerMessage(LogLevel.Warning, "Failed to flush {count} hit batches")]
    static partial void LogFlushFailed(ILogger<HitFlushService> logger, Exception exception, int count);
}
