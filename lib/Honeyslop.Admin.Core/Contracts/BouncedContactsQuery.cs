namespace Honeyslop.Admin.Core.Contracts;

public sealed record BouncedContactsQuery(int Days = 0,
    string? Search = null,
    int Page = 0);