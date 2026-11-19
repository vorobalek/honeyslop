using Honeyslop.Admin.Core.Contracts;

namespace Honeyslop.Admin.Views.Contracts;

public sealed record RequestsViewDto(
    PagedDto<RequestRowDto> Requests,
    IReadOnlyDictionary<string, CountryDto> Countries,
    IReadOnlyDictionary<string, int> Delays,
    IReadOnlyDictionary<string, int> LearnedDelays,
    DateTimeOffset AsOf);