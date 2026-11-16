namespace Honeyslop.Trap.Hits;

public sealed record HitBatch(long EntryId, long Count, DateTimeOffset LastHitAt);
