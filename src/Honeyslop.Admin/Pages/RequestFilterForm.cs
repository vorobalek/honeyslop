using Honeyslop.Data.Enums;

namespace Honeyslop.Admin.Pages;

public sealed record RequestFilterForm(
    string? ClientIp,
    bool ClientIpLocked,
    string? Host,
    string? Target,
    RequestOutcome? Outcome,
    long? EntryId,
    string ResetUrl);
