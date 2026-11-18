using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Honeyslop.Admin.Pages;

public sealed class GeneratorsModel(IGeneratorsService generatorsService) : PageModel
{
    public GeneratorsDto Overview { get; private set; } = null!;
    public bool Configured => Overview.Configured;
    public GeneratorDto[] Generators => Overview.Generators;
    public EntryTotals Totals => Overview.Totals;
    public DateTimeOffset? OldestQueuedAt => Overview.OldestQueuedAt;
    public Entry[] Generating => Overview.Generating;
    public Entry[] Queued => Overview.Queued;
    public DateTimeOffset RenderedAt => Overview.AsOf;
    public int TotalSlots => Overview.TotalSlots;
    public int BusySlots => Overview.BusySlots;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Overview = await generatorsService.GetOverviewAsync(cancellationToken);
    }

    public async Task<IActionResult> OnGetPanelAsync(CancellationToken cancellationToken)
    {
        Overview = await generatorsService.GetOverviewAsync(cancellationToken);
        return Partial("_GeneratorsPanel", this);
    }
}
