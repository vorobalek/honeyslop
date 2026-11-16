using Honeyslop.Data.Entities;
using Honeyslop.Data.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Honeyslop.Admin.Pages;

public sealed class RequestModel(IRequestRepository requestRepository) : PageModel
{
    public RequestRecord Record { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var record = await requestRepository.GetByIdOrDefaultAsync(id, cancellationToken);
        if (record is null)
            return NotFound();
        Record = record;
        return Page();
    }
}
