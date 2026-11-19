using Honeyslop.Data.Enums;

namespace Honeyslop.Admin.Views.Contracts;

public sealed record RequestRowDto(
    long Id,
    DateTimeOffset ReceivedAt,
    string ClientIp,
    string Method,
    string Host,
    string Target,
    long? EntryId,
    RequestOutcome Outcome,
    int ResponseStatus,
    int DurationMs,
    string? UserAgent);
