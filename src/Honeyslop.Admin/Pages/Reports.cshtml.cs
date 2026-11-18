using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Enums;
using Honeyslop.Admin.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace Honeyslop.Admin.Pages;

public sealed class ReportsModel(IReportsService reportsService) : SortedListModel<ReportColumn>
{
    [BindProperty(SupportsGet = true, Name = "p")]
    public int PageNumber { get; set; }

    public ReportsPageDto View { get; private set; } = null!;
    public ReportDto[] Reports => View.Reports.Items;
    public IReadOnlyDictionary<string, CountryDto> Countries => View.Countries;
    public int TotalCount => View.Reports.Total;
    public int PageCount => View.Reports.PageCount;

    public Task OnGetAsync(CancellationToken cancellationToken)
    {
        return LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnGetPanelAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        return Partial("_ReportsList", this);
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        View = await reportsService.GetPageAsync(new ReportsQuery(Sorting, PageNumber), cancellationToken);
        PageNumber = View.Reports.Page;
    }

    public override string ListPage => "/Reports";

    protected override Dictionary<string, string> FilterValues()
    {
        return [];
    }
}
