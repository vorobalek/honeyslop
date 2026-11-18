namespace Honeyslop.Admin.Clients;

public sealed record WhoisInfo(
    string? Prefix,
    WhoisOrigin[] Origins,
    string? Network,
    string? NetworkName,
    string? Owner,
    string? Country,
    string? Registry,
    string[] AbuseContacts,
    string Records,
    DateTimeOffset LookedUpAt);

public sealed record WhoisOrigin(long Asn, string Holder);
