using Honeyslop.Data.Entities;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record RequestsPageDto(PagedDto<RequestRecord> Requests,
    IReadOnlyDictionary<string, CountryDto> Countries,
    IReadOnlyDictionary<string, int> Delays,
    IReadOnlyDictionary<string, int> LearnedDelays,
    DateTimeOffset AsOf);