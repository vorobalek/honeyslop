
namespace Honeyslop.Admin.Core.Contracts;

public sealed record ReportsPageDto(PagedDto<ReportDto> Reports, IReadOnlyDictionary<string, CountryDto> Countries, DateTimeOffset AsOf);
