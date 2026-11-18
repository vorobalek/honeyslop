using System.Collections.Concurrent;

namespace Honeyslop.Trap.Hits.Internals;

internal sealed class HitCounter : IHitCounter
{
    private readonly ConcurrentDictionary<long, HitBatch> _batches = new();

    public void Add(long entryId, DateTimeOffset hitAt)
    {
        _batches.AddOrUpdate(
            entryId,
            static (id, at) => new HitBatch(id, 1, at),
            static (_, batch, at) => batch with
            {
                Count = batch.Count + 1,
                LastHitAt = at > batch.LastHitAt ? at : batch.LastHitAt
            },
            hitAt);
    }

    public IReadOnlyCollection<HitBatch> Drain()
    {
        var drained = new List<HitBatch>();
        foreach (var entryId in _batches.Keys)
            if (_batches.TryRemove(entryId, out var batch))
                drained.Add(batch);
        return drained;
    }
}
