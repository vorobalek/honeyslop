
namespace Honeyslop.Admin.Core.Contracts;

public sealed record ReportDetailsDto(ReportDto Report, CountryDto? Country, string Body, string? MessageId, string? From, bool FromIsExpected);
