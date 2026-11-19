using Honeyslop.Admin.Core.Contracts;

namespace Honeyslop.Admin.Views.Contracts;

public sealed record RequestsViewDto(
    PagedDto<RequestRowDto> Requests,
    IReadOnlyDictionary<string, CountryDto> Countries,
    DateTimeOffset AsOf);