using Honeyslop.Admin.Clients;
using Honeyslop.Admin.Extensions;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Honeyslop.Admin.Pages;

public sealed class ReportModel(
    IAbuseReportRepository abuseReportRepository,
    IOptions<AbuseReportOptions> reportOptions) : PageModel
{
    public AbuseReport Report { get; private set; } = null!;

    public bool FromIsExpected => Report.SentFrom is null;

    public string From => Report.SentFrom ??
                          reportOptions.Value.ExpectedFrom(Report.SenderName) ??
                          Report.SenderName ??
                          "the address only";

    public string Attachment => Report.AttachedRequests is { } attached
        ? $"{attached.Of("request")} from this address, zipped; built when sending and not kept"
        : "—";

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var report = await abuseReportRepository.GetByIdOrDefaultAsync(id, cancellationToken);
        if (report is null)
            return NotFound();
        Report = report;
        return Page();
    }
}
