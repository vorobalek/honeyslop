using Honeyslop.Data.Entities;
using Honeyslop.Data.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Honeyslop.Admin.Pages;

public sealed class ReportsModel(IAbuseReportRepository abuseReportRepository) : PageModel
{
    public const int PageSize = 50;

    [BindProperty(SupportsGet = true, Name = "p")]
    public int PageNumber { get; set; }

    public AbuseReport[] Reports { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int PageCount => Math.Max(1, (TotalCount + PageSize - 1) / PageSize);

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
        PageNumber = Math.Max(0, PageNumber);
        TotalCount = await abuseReportRepository.CountAsync(cancellationToken);
        Reports = await abuseReportRepository.GetPageAsync(PageNumber, PageSize, cancellationToken);
    }
}
