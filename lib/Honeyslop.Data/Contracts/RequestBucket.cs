namespace Honeyslop.Data.Contracts;

public sealed record RequestBucket
{
    public string Key { get; init; } = null!;
    public int Requests { get; init; }
    public int Clients { get; init; }
    public DateTimeOffset FirstSeenAt { get; init; }
    public DateTimeOffset LastSeenAt { get; init; }
}
