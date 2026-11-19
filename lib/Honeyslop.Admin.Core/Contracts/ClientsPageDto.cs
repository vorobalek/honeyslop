using Honeyslop.Data.Contracts;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record ClientsPageDto(PagedDto<ClientActivity> Clients,
    IReadOnlyDictionary<string, CountryDto> Countries,
    DateTimeOffset AsOf);