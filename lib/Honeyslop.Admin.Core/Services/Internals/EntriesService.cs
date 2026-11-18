using Honeyslop.Admin.Core.Access;
using Honeyslop.Admin.Core.Clients;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Extensions;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Llm.Contracts;

namespace Honeyslop.Admin.Core.Services.Internals;

internal sealed class EntriesService(
    IEntryRepository entryRepository,
    IGenerationOutputStore outputStore,
    ICountryLookup countryLookup,
    IAccessGuard access,
    IServerTimeRepository serverTime) : IEntriesService
{
    public async Task<EntriesPageDto> GetPageAsync(EntriesQuery query, CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        var page = Math.Max(0, query.Page);
        var asOf = await serverTime.GetNowAsync(cancellationToken);
        var (host, target, state) = query.Filter;
        var (order, direction) = query.Sort.ToOrder();
        var totals = await entryRepository.GetTotalsAsync(cancellationToken);
        var total = await entryRepository.CountAsync(host, target, state, cancellationToken);
        var entries = await entryRepository.GetPageAsync(host, target, state, order, direction, page, IEntriesService.PageSize, cancellationToken);
        return new EntriesPageDto(totals, new PagedDto<Entry>(entries, total, page, IEntriesService.PageSize), asOf);
    }

    public async Task<EntryDetailsDto?> GetAsync(long id, CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        var entry = await entryRepository.GetByIdOrDefaultAsync(id, cancellationToken);
        if (entry is null)
            return null;
        var current = entry.Status == EntryStatus.Expired
            ? await entryRepository.GetByKeyHashOrDefaultAsync(entry.KeyHash, cancellationToken)
            : null;
        return new EntryDetailsDto(
            entry,
            entry.ClientIp is null ? null : countryLookup.Find(entry.ClientIp),
            current?.Id,
            entry.State == EntryState.Generating ? outputStore.GetOrDefault(entry.Id) : null,
            await serverTime.GetNowAsync(cancellationToken));
    }

    public async Task<GenerationOutputDto?> GetOutputAsync(long id, CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        return outputStore.GetOrDefault(id);
    }

    public async Task<string?> GetPreviewAsync(long id, CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        return (await entryRepository.GetByIdOrDefaultAsync(id, cancellationToken))?.ResponseBody;
    }

    public async Task<int> ExpireAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken)
    {
        access.EnsureCanOperate();
        return ids.Count > 0
            ? await entryRepository.ExpireAsync(ids, await serverTime.GetNowAsync(cancellationToken), cancellationToken)
            : 0;
    }

    public async Task<int> ExpireMatchingAsync(EntryFilter filter, CancellationToken cancellationToken)
    {
        access.EnsureCanOperate();
        return await entryRepository.ExpireMatchingAsync(filter.Host, filter.Target, filter.State, await serverTime.GetNowAsync(cancellationToken), cancellationToken);
    }
}
