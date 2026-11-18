using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Honeyslop.Admin.Pages;

public sealed class RequestModel(IRequestsService requestsService) : PageModel
{
    public RequestDetailsDto Details { get; private set; } = null!;
    public RequestRecord Record => Details.Request;

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var record = await requestsService.GetAsync(id, cancellationToken);
        if (record is null)
            return NotFound();
        Details = record;
        return Page();
    }
}
