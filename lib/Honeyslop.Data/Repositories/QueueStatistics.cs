namespace Honeyslop.Data.Repositories;

public sealed record QueueStatistics(
    int Queued,
    int Failed,
    DateTimeOffset? OldestQueuedAt,
    IReadOnlyDictionary<string, int> GeneratingByInstance)
{
    public int Generating => GeneratingByInstance.Values.Sum();
}
