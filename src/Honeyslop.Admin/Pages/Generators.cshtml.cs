using Honeyslop.Data.Repositories;
using Honeyslop.Llm;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Honeyslop.Admin.Pages;

public sealed class GeneratorsModel(
    ILlmEndpointProvider endpointProvider,
    ILlmClient llmClient,
    IEntryRepository entryRepository,
    IOptions<LlmOptions> llmOptions,
    TimeProvider timeProvider) : PageModel
{

    public bool Configured => llmOptions.Value.Urls.Count > 0;
    public GeneratorRow[] Generators { get; private set; } = [];
    public QueueStatistics Queue { get; private set; } = new(0, 0, null, new Dictionary<string, int>());
    public EntryProgress[] Generating { get; private set; } = [];
    public EntryProgress[] Queued { get; private set; } = [];
    public DateTimeOffset RenderedAt { get; private set; }
    public int TotalSlots => Generators.Sum(x => x.Status?.Slots ?? 0);
    public int BusySlots => Generators.Sum(x => x.Status?.BusySlots ?? 0);

    public Task OnGetAsync(CancellationToken cancellationToken)
    {
        return LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnGetPanelAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        return Partial("_GeneratorsPanel", this);
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        RenderedAt = timeProvider.GetUtcNow();
        Queue = await entryRepository.GetQueueStatisticsAsync(cancellationToken);
        Generating = await entryRepository.GetGeneratingAsync(50, cancellationToken);
        Queued = await entryRepository.GetQueuedAsync(20, cancellationToken);
        var statistics = await entryRepository.GetGeneratorStatisticsAsync(RenderedAt - GeneratorStatistics.Window, cancellationToken);
        var endpoints = Configured
            ? await endpointProvider.GetEndpointsAsync(cancellationToken)
            : [];
        var statuses = await Task.WhenAll(endpoints.Select(x => llmClient.GetStatusAsync(x, cancellationToken)));

        var rows = endpoints
            .Select((endpoint, index) => Row(endpoint.Authority, statuses[index], true, statistics))
            .ToList();
        var known = rows.Select(x => x.Authority).ToHashSet(StringComparer.Ordinal);
        rows.AddRange(statistics
            .Select(x => AuthorityOf(x.Model))
            .Where(x => !known.Contains(x))
            .Distinct(StringComparer.Ordinal)
            .Select(x => Row(x, null, false, statistics)));
        Generators = [..rows];
    }

    private static GeneratorRow Row(
        string authority,
        LlmServerStatus? status,
        bool discovered,
        IReadOnlyCollection<GeneratorStatistics> statistics)
    {
        var own = statistics
            .Where(x => AuthorityOf(x.Model) == authority)
            .ToArray();
        var generated = own.Sum(x => x.Generated);
        return new GeneratorRow(
            authority,
            status,
            discovered,
            generated,
            generated > 0 ? own.Sum(x => x.AverageMs * x.Generated) / generated : 0,
            generated > 0 ? own.Sum(x => x.AverageTokens * x.Generated) / generated : 0,
            own.Max(x => x.LastGeneratedAt));
    }

    private static string AuthorityOf(string model)
    {
        var at = model.LastIndexOf('@');
        return at >= 0 ? model[(at + 1)..] : model;
    }

    public sealed record GeneratorRow(
        string Authority,
        LlmServerStatus? Status,
        bool Discovered,
        int Generated,
        double AverageMs,
        double AverageTokens,
        DateTimeOffset? LastGeneratedAt);
}
