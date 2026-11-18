namespace Honeyslop.Data.Contracts;

public sealed record ClientActivity
{
    public string ClientIp { get; init; } = null!;
    public int Requests { get; init; }
    public int Hosts { get; init; }
    public int Targets { get; init; }
    public DateTimeOffset FirstSeenAt { get; init; }
    public DateTimeOffset LastSeenAt { get; init; }
    public string? LastUserAgent { get; init; }
}
