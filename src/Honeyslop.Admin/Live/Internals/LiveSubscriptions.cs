using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Honeyslop.Admin.Live.Contracts;

namespace Honeyslop.Admin.Live.Internals;

internal sealed class LiveSubscriptions(
    IServiceScopeFactory scopeFactory,
    ILiveSender sender,
    TimeProvider timeProvider,
    ILogger<LiveSubscriptions> logger) : BackgroundService, ILiveSubscriptions
{
    private static readonly TimeSpan PeriodicInterval = TimeSpan.FromSeconds(5);

    private readonly Lock _lock = new();
    private readonly Dictionary<string, LiveView> _views = [];
    private readonly Dictionary<(string ConnectionId, string Key), LiveView> _memberships = [];

    public int Count
    {
        get
        {
            lock (_lock)
                return _views.Count;
        }
    }

    public async Task WatchAsync(LiveWatch watch, CancellationToken cancellationToken)
    {
        var group = GroupOf(watch);
        LiveView view;
        LiveView? left;
        lock (_lock)
        {
            left = Leave(watch.ConnectionId, watch.Key, group);
            if (!_views.TryGetValue(group, out view!))
            {
                view = new LiveView(group, watch.Key, watch.User, watch.Interest, watch.Build, scopeFactory, sender, logger);
                _views[group] = view;
            }

            view.Members.Add(watch.ConnectionId);
            _memberships[(watch.ConnectionId, watch.Key)] = view;
        }

        if (left is not null)
            await sender.LeaveAsync(watch.ConnectionId, left.Group, cancellationToken);
        await sender.JoinAsync(watch.ConnectionId, group, cancellationToken);
        view.Invalidate();
    }

    public async Task UnwatchAsync(string connectionId, string key, CancellationToken cancellationToken)
    {
        LiveView? left;
        lock (_lock)
            left = Leave(connectionId, key, null);
        if (left is not null)
            await sender.LeaveAsync(connectionId, left.Group, cancellationToken);
    }

    public void Drop(string connectionId)
    {
        lock (_lock)
            foreach (var membership in _memberships.Keys.Where(x => x.ConnectionId == connectionId).ToArray())
                Leave(membership.ConnectionId, membership.Key, null);
    }

    public void Publish(LiveChange change)
    {
        foreach (var view in Snapshot(x => x.Interest.Matches(change)))
            view.Invalidate();
    }

    public void PublishEverything()
    {
        foreach (var view in Snapshot(_ => true))
            view.Invalidate();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PeriodicInterval, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                foreach (var view in Snapshot(x => x.Interest.Periodic))
                    view.Invalidate();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private LiveView? Leave(string connectionId, string key, string? stayIn)
    {
        if (!_memberships.TryGetValue((connectionId, key), out var view) || view.Group == stayIn)
            return null;
        _memberships.Remove((connectionId, key));
        view.Members.Remove(connectionId);
        if (view.Members.Count > 0)
            return view;
        _views.Remove(view.Group);
        view.Dispose();
        return view;
    }

    private LiveView[] Snapshot(Func<LiveView, bool> predicate)
    {
        lock (_lock)
            return [.. _views.Values.Where(predicate)];
    }

    private static string GroupOf(LiveWatch watch)
    {
        var signature = JsonSerializer.Serialize(new[] { watch.Audience, watch.Key, JsonSerializer.Serialize(watch.Request) });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(signature)));
    }
}
