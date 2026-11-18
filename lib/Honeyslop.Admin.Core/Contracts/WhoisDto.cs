namespace Honeyslop.Admin.Core.Contracts;

public sealed record WhoisDto(
    string? Prefix,
    WhoisOriginDto[] Origins,
    string? Network,
    string? NetworkName,
    string? Owner,
    string? Country,
    string? Registry,
    string[] AbuseContacts,
    string Records,
    DateTimeOffset LookedUpAt);
