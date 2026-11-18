using Honeyslop.Admin.Core.Access;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Repositories;
using Honeyslop.Llm;
using Honeyslop.Llm.Contracts;
using Microsoft.Extensions.Options;

namespace Honeyslop.Admin.Core.Services.Internals;

internal sealed class GeneratorsService(
    ILlmEndpointProvider endpointProvider,
    ILlmClient llmClient,
    IEntryRepository entryRepository,
    IOptions<LlmOptions> llmOptions,
    IAccessGuard access,
    IServerTimeRepository serverTime) : IGeneratorsService
{
    public async Task<GeneratorsDto> GetOverviewAsync(CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        var asOf = await serverTime.GetNowAsync(cancellationToken);
        var configured = llmOptions.Value.Urls.Count > 0;
        var totals = await entryRepository.GetTotalsAsync(cancellationToken);
        var oldestQueuedAt = await entryRepository.GetOldestQueuedAtAsync(cancellationToken);
        var generating = await entryRepository.GetGeneratingAsync(IGeneratorsService.GeneratingLimit, cancellationToken);
        var queued = await entryRepository.GetQueuedAsync(IGeneratorsService.QueuedLimit, cancellationToken);
        var statistics = await entryRepository.GetGeneratorActivityAsync(asOf - IGeneratorsService.ProductionWindow, cancellationToken);
        var endpoints = configured
            ? await endpointProvider.GetEndpointsAsync(cancellationToken)
            : [];
        var statuses = await Task.WhenAll(endpoints.Select(x => llmClient.GetStatusAsync(x.Url, cancellationToken)));

        var rows = endpoints
            .Select((endpoint, index) => Row(endpoint.Name, endpoint.Url.Authority, statuses[index], true, statistics))
            .ToList();
        var known = rows.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        rows.AddRange(statistics
            .Select(x => x.Generator)
            .Where(x => !known.Contains(x))
            .Distinct(StringComparer.Ordinal)
            .Select(x => Row(x, null, null, false, statistics)));
        return new GeneratorsDto(configured, [.. rows], totals, oldestQueuedAt, generating, queued, asOf);
    }

    private static GeneratorDto Row(
        string name,
        string? address,
        LlmServerStatus? status,
        bool discovered,
        IReadOnlyCollection<GeneratorActivity> statistics)
    {
        var own = statistics
            .Where(x => x.Generator == name)
            .ToArray();
        var generated = own.Sum(x => x.Generated);
        return new GeneratorDto(
            name,
            address,
            status,
            discovered,
            generated,
            generated > 0 ? own.Sum(x => x.AverageMs * x.Generated) / generated : 0,
            generated > 0 ? own.Sum(x => x.AverageTokens * x.Generated) / generated : 0,
            own.Max(x => x.LastGeneratedAt));
    }
}
