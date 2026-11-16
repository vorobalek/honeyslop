namespace Honeyslop.Data.Repositories;

public sealed record ClientSummary(
    string ClientIp,
    int Requests,
    int Hosts,
    int Targets,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt,
    string? LastUserAgent);
