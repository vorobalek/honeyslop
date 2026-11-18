using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;

namespace Honeyslop.Admin.Live.Internals;

internal sealed class LiveUpdateHub(TimeProvider timeProvider) : BackgroundService, ILiveUpdateHub
{
    private const int MaxEntryIds = 200;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMilliseconds(250);

    private readonly ConcurrentDictionary<Guid, Channel<string>> _subscribers = new();
    private readonly Lock _lock = new();
    private readonly HashSet<string> _topics = [];
    private readonly Dictionary<string, HashSet<long>> _ids = new()
    {
        [LiveTopics.Entries] = [],
        [LiveTopics.Outputs] = []
    };

    public int Subscribers => _subscribers.Count;

    public LiveSubscription Subscribe()
    {
        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(16)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });
        var id = Guid.NewGuid();
        _subscribers[id] = channel;
        return new LiveSubscription(channel.Reader, () => _subscribers.TryRemove(id, out _));
    }

    public void Publish(string topic, long? entryId = null)
    {
        lock (_lock)
        {
            _topics.Add(topic);
            if (entryId is { } id && _ids.TryGetValue(topic, out var ids) && ids.Count < MaxEntryIds)
                ids.Add(id);
        }
    }

    public void PublishEverything()
    {
        Publish(LiveTopics.Entries);
        Publish(LiveTopics.Requests);
        Publish(LiveTopics.Reports);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(FlushInterval, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                Flush();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private void Flush()
    {
        string message;
        lock (_lock)
        {
            if (_topics.Count == 0)
                return;
            message = JsonSerializer.Serialize(new
            {
                topics = _topics.ToArray(),
                entries = _ids[LiveTopics.Entries].ToArray(),
                outputs = _ids[LiveTopics.Outputs].ToArray()
            });
            _topics.Clear();
            foreach (var ids in _ids.Values)
                ids.Clear();
        }

        foreach (var channel in _subscribers.Values)
            channel.Writer.TryWrite(message);
    }
}
