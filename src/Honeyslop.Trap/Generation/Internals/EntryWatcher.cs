using System.Collections.Concurrent;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Trap.Monitoring;
using Honeyslop.Trap.Options;
using Honeyslop.Trap.Responses;
using Microsoft.Extensions.Options;

namespace Honeyslop.Trap.Generation.Internals;

internal sealed partial class EntryWatcher(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<TrapOptions> options,
    TimeProvider timeProvider,
    ILogger<EntryWatcher> logger) : BackgroundService, IEntryWatcher
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxWatch = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<long, Watch> _watches = new();
    private int _waiters;

    public async Task<CachedResponse?> WaitAsync(long entryId, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (timeout <= TimeSpan.Zero)
            return null;

        if (Interlocked.Increment(ref _waiters) > options.Value.MaxWaiters)
        {
            Interlocked.Decrement(ref _waiters);
            return null;
        }

        TrapMetrics.Waiters.Inc();
        try
        {
            var watch = _watches.GetOrAdd(
                entryId,
                static (_, now) => new Watch(new TaskCompletionSource<CachedResponse?>(TaskCreationOptions.RunContinuationsAsynchronously), now),
                timeProvider.GetUtcNow());
            return await watch.Completion.Task.WaitAsync(timeout, timeProvider, cancellationToken);
        }
        catch (TimeoutException)
        {
            return null;
        }
        finally
        {
            Interlocked.Decrement(ref _waiters);
            TrapMetrics.Waiters.Dec();
        }
    }

    public void Complete(long entryId, CachedResponse? response)
    {
        if (_watches.TryRemove(entryId, out var watch))
            watch.Completion.TrySetResult(response);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await PollAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    internal async Task PollAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        foreach (var (entryId, watch) in _watches)
            if (now - watch.Since > MaxWatch)
                Complete(entryId, null);

        var entryIds = _watches.Keys.ToArray();
        if (entryIds.Length == 0)
            return;

        try
        {
            await using var asyncScope = serviceScopeFactory.CreateAsyncScope();
            var finished = await asyncScope
                .ServiceProvider
                .GetRequiredService<IEntryRepository>()
                .GetFinishedAsync(entryIds, cancellationToken);
            foreach (var entry in finished)
                if (entry.Status == EntryStatus.Ready)
                    Complete(entry.Id, CachedResponse.From(entry));
                else if (entry.Status == EntryStatus.Expired || entry.Attempts >= options.Value.MaxAttempts)
                    Complete(entry.Id, null);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogPollFailed(logger, exception, entryIds.Length);
        }
    }

    [LoggerMessage(LogLevel.Warning, "Failed to check {count} awaited entries")]
    static partial void LogPollFailed(ILogger<EntryWatcher> logger, Exception exception, int count);

    private sealed record Watch(TaskCompletionSource<CachedResponse?> Completion, DateTimeOffset Since);
}
