using Honeyslop.Data.Enums;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record RequestFilter(
    string? ClientIp = null,
    string? Host = null,
    string? Target = null,
    RequestOutcome? Outcome = null,
    long? EntryId = null,
    long? AfterId = null,
    string? Method = null,
    string? UserAgent = null,
    DateTimeOffset? Since = null,
    DateTimeOffset? Until = null);
