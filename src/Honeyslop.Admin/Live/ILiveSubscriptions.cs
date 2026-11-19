using Honeyslop.Admin.Live.Contracts;

namespace Honeyslop.Admin.Live;

public interface ILiveSubscriptions
{
    int Count { get; }

    Task WatchAsync(LiveWatch watch, CancellationToken cancellationToken);
    Task UnwatchAsync(string connectionId, string key, CancellationToken cancellationToken);
    void Drop(string connectionId);
    void Publish(LiveChange change);
    void PublishEverything();
}