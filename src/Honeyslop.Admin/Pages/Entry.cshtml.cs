using Honeyslop.Admin.Live;
using Honeyslop.Data.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EntryEntity = Honeyslop.Data.Entities.Entry;

namespace Honeyslop.Admin.Pages;

public sealed class EntryModel(
    IEntryRepository entryRepository,
    GenerationOutputStore outputStore,
    TimeProvider timeProvider) : PageModel
{
    private const string PreviewContentSecurityPolicy =
        "sandbox; default-src 'none'; img-src data:; style-src 'unsafe-inline'; font-src data:; frame-ancestors 'self'";

    public EntryEntity Entry { get; private set; } = null!;
    public GenerationOutput? Output { get; private set; }
    public DateTimeOffset Now { get; } = timeProvider.GetUtcNow();

    public string? ContentType => Entry
        .ResponseHeaders
        .FirstOrDefault(x => x.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))?
        .Value;

    public bool CanPreview =>
        !string.IsNullOrEmpty(Entry.ResponseBody) &&
        ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true;

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var entry = await entryRepository.GetByIdOrDefaultAsync(id, cancellationToken);
        if (entry is null)
            return NotFound();
        Entry = entry;
        Output = outputStore.GetOrDefault(id);
        return Page();
    }

    public IActionResult OnGetPanel(long id)
    {
        Output = outputStore.GetOrDefault(id);
        return Partial("_OutputPanel", this);
    }

    public async Task<IActionResult> OnGetPreviewAsync(long id, CancellationToken cancellationToken)
    {
        var entry = await entryRepository.GetByIdOrDefaultAsync(id, cancellationToken);
        if (entry?.ResponseBody is null)
            return NotFound();

        Response.Headers.ContentSecurityPolicy = PreviewContentSecurityPolicy;
        return Content(entry.ResponseBody, "text/html; charset=utf-8");
    }

    public async Task<IActionResult> OnPostExpireAsync(long id, CancellationToken cancellationToken)
    {
        var expired = await entryRepository.DeleteAsync([id], cancellationToken);
        return RedirectToPage("/Index", new
        {
            Expired = expired
        });
    }
}
