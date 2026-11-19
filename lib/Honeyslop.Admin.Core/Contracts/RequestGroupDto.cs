namespace Honeyslop.Admin.Core.Contracts;

public sealed record RequestGroupDto(
    string Key,
    CountryDto? Country,
    int Requests,
    int Clients,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt);