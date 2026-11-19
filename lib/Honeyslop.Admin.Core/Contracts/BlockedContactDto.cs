namespace Honeyslop.Admin.Core.Contracts;

public sealed record BlockedContactDto(string Contact,
    string? Note,
    string? CreatedBy,
    DateTimeOffset CreatedAt);