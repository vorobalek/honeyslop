namespace Honeyslop.Data.Contracts;

public sealed record EntryTotals
{
    public int Total { get; init; }
    public int Ready { get; init; }
    public int Queued { get; init; }
    public int Generating { get; init; }
    public int Failed { get; init; }
    public int Expired { get; init; }
    public long Hits { get; init; }
}
