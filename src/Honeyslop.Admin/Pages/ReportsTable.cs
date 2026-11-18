using Honeyslop.Admin.Core.Contracts;

namespace Honeyslop.Admin.Pages;

public sealed record ReportsTable(
    ReportDto[] Reports,
    IReadOnlyDictionary<string, CountryDto> Countries,
    bool ShowClient,
    bool ShowRequester,
    ReportsModel? List = null);
