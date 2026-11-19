namespace Honeyslop.Admin.Core.Contracts;

public sealed record BlockedContactsQuery(string? Search = null,
    int Page = 0);