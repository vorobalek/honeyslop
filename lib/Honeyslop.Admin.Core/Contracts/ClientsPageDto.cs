using Honeyslop.Data.Contracts;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record ClientsPageDto(PagedDto<ClientActivity> Clients,
    IReadOnlyDictionary<string, CountryDto> Countries,
    IReadOnlyDictionary<string, int> Delays,
    IReadOnlyDictionary<string, int> LearnedDelays,
    DateTimeOffset AsOf);