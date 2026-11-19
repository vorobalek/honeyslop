namespace Honeyslop.Trap.Hits;

public interface IHitCounter
{
    void Add(long entryId, DateTimeOffset hitAt);
    IReadOnlyCollection<HitBatch> Drain();
}