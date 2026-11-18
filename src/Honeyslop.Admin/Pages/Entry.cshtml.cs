using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Data.Entities;
using Honeyslop.Llm.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Honeyslop.Admin.Pages;

public sealed class EntryModel(IEntriesService entriesService) : PageModel
{
    private const string PreviewContentSecurityPolicy =
        "sandbox; default-src 'none'; img-src data:; style-src 'unsafe-inline'; font-src data:; frame-ancestors 'self'";

    public EntryDetailsDto Details { get; private set; } = null!;
    public Entry Entry => Details.Entry;
    public GenerationOutputDto? Output { get; private set; }
    public DateTimeOffset Now => Details.AsOf;

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var entry = await entriesService.GetAsync(id, cancellationToken);
        if (entry is null)
            return NotFound();
        Details = entry;
        Output = entry.Output;
        return Page();
    }

    public async Task<IActionResult> OnGetPanelAsync(long id, CancellationToken cancellationToken)
    {
        Output = await entriesService.GetOutputAsync(id, cancellationToken);
        return Partial("_OutputPanel", this);
    }

    public async Task<IActionResult> OnGetPreviewAsync(long id, CancellationToken cancellationToken)
    {
        var body = await entriesService.GetPreviewAsync(id, cancellationToken);
        if (body is null)
            return NotFound();

        Response.Headers.ContentSecurityPolicy = PreviewContentSecurityPolicy;
        return Content(body, "text/html; charset=utf-8");
    }

    public async Task<IActionResult> OnPostExpireAsync(long id, CancellationToken cancellationToken)
    {
        var expired = await entriesService.ExpireAsync([id], cancellationToken);
        return RedirectToPage("/Index", new
        {
            Expired = expired
        });
    }
}
