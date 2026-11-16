using Honeyslop.Data.Enums;

namespace Honeyslop.Data.Repositories;

public sealed record RequestSummary(
    long Id,
    DateTimeOffset ReceivedAt,
    string ClientIp,
    string Method,
    string Host,
    string Target,
    string? UserAgent,
    RequestOutcome Outcome,
    int ResponseStatus,
    long? EntryId,
    int DurationMs);
