using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Admin.Core.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Honeyslop.Admin.Pages;

public sealed class ReportModel(IReportsService reportsService) : PageModel
{
    public ReportDetailsDto Details { get; private set; } = null!;
    public ReportDto Report => Details.Report;

    public string Attachment => Report.AttachedRequests is { } attached
        ? $"{attached.Of("request")} from this address, zipped; built when sending and not kept"
        : "—";

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var details = await reportsService.GetAsync(id, cancellationToken);
        if (details is null)
            return NotFound();
        Details = details;
        return Page();
    }
}
