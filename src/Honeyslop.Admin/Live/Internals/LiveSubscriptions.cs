using System.Collections.Concurrent;
using System.Security.Claims;
using Honeyslop.Admin.Live.Contracts;
using Honeyslop.Admin.Views;

namespace Honeyslop.Admin.Live.Internals;

internal sealed class LiveSubscriptions(
    IServiceScopeFactory scopeFactory,
    ILiveSender sender,
    TimeProvider timeProvider,
    ILogger<LiveSubscriptions> logger) : BackgroundService, ILiveSubscriptions
{
    private static readonly TimeSpan PeriodicInterval = TimeSpan.FromSeconds(5);

    private readonly ConcurrentDictionary<(string ConnectionId, int WatchId), LiveSubscription> _subscriptions = new();

    public int Count => _subscriptions.Count;

    public void Watch(
        string connectionId,
        int watchId,
        ClaimsPrincipal user,
        LiveInterest interest,
        Func<IViewBuilder, CancellationToken, Task<object?>> build)
    {
        var subscription = new LiveSubscription(connectionId, watchId, user, interest, build, scopeFactory, sender, logger);
        _subscriptions.AddOrUpdate(
            (connectionId, watchId),
            subscription,
            (_, previous) =>
            {
                previous.Dispose();
                return subscription;
            });
        subscription.Invalidate();
    }

    public void Unwatch(string connectionId, int watchId)
    {
        if (_subscriptions.TryRemove((connectionId, watchId), out var subscription))
            subscription.Dispose();
    }

    public void Drop(string connectionId)
    {
        foreach (var key in _subscriptions.Keys.Where(x => x.ConnectionId == connectionId))
            Unwatch(key.ConnectionId, key.WatchId);
    }

    public void Publish(LiveChange change)
    {
        foreach (var subscription in _subscriptions.Values.Where(x => x.Interest.Matches(change)))
            subscription.Invalidate();
    }

    public void PublishEverything()
    {
        foreach (var subscription in _subscriptions.Values)
            subscription.Invalidate();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PeriodicInterval, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                foreach (var subscription in _subscriptions.Values.Where(x => x.Interest.Periodic))
                    subscription.Invalidate();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
