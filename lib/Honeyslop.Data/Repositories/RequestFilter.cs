using Honeyslop.Data.Enums;

namespace Honeyslop.Data.Repositories;

public sealed record RequestFilter(
    string? ClientIp = null,
    string? Host = null,
    string? Target = null,
    RequestOutcome? Outcome = null,
    long? EntryId = null);
