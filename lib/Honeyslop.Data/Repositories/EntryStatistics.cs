namespace Honeyslop.Data.Repositories;

public sealed record EntryStatistics(
    int Total,
    int Ready,
    int Queued,
    int Generating,
    int Failed,
    int Expired,
    long Hits);
