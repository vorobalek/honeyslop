using Honeyslop.Data.Entities;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record RequestsPageDto(PagedDto<RequestRecord> Requests, IReadOnlyDictionary<string, CountryDto> Countries, DateTimeOffset AsOf);
