using Honeyslop.Admin.Core.Access;
using Honeyslop.Admin.Core.Clients;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Enums;
using Honeyslop.Admin.Core.Extensions;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;

namespace Honeyslop.Admin.Core.Services.Internals;

internal sealed class RequestsService(
    IRequestRepository requestRepository,
    IClientDelayRepository clientDelayRepository,
    IClientProbeRepository clientProbeRepository,
    ISettingsRepository settingsRepository,
    ICountryLookup countryLookup,
    IAccessGuard access,
    IServerTimeRepository serverTime) : IRequestsService
{
    public async Task<RequestsPageDto> GetPageAsync(RequestsQuery query, CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        var page = Math.Max(0, query.Page);
        var asOf = await serverTime.GetNowAsync(cancellationToken);
        var total = await requestRepository.CountAsync(query.Filter, cancellationToken);
        var requests = await requestRepository.GetPageAsync(query.Filter, query.Sort, page, IRequestsService.PageSize, cancellationToken);
        return new RequestsPageDto(
            new PagedDto<RequestRecord>(requests, total, page, IRequestsService.PageSize),
            countryLookup.FindAll(requests.Select(x => x.ClientIp)),
            await clientDelayRepository.FindAllAsync(requests.Select(x => x.ClientIp), cancellationToken),
            (await settingsRepository.GetAsync(null, cancellationToken)).Honeyslop.AutoDelayEnabled
                ? await clientProbeRepository.FindAllAsync(requests.Select(x => x.ClientIp), cancellationToken)
                : new Dictionary<string, int>(),
            asOf);
    }

    public async Task<RequestDetailsDto?> GetAsync(long id, CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        var record = await requestRepository.GetByIdOrDefaultAsync(id, cancellationToken);
        return record is null ? null : new RequestDetailsDto(record, countryLookup.Find(record.ClientIp));
    }

    public async Task<RequestsBreakdownDto> GetBreakdownAsync(RequestsBreakdownQuery query, CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        var asOf = await serverTime.GetNowAsync(cancellationToken);
        var top = Math.Clamp(query.Top, 1, IRequestsService.MaxBreakdownGroups);
        var total = await requestRepository.CountAsync(query.Filter, cancellationToken);
        var groups = query.By switch
        {
            BreakdownDimension.Country => ByCountry(
                await requestRepository.GetBreakdownAsync(query.Filter, RequestDimension.Client, int.MaxValue, cancellationToken),
                top),
            BreakdownDimension.Client =>
            [
                .. (await requestRepository.GetBreakdownAsync(query.Filter, RequestDimension.Client, top, cancellationToken))
                .Select(x => Group(x, countryLookup.Find(x.Key)))
            ],
            var by =>
            [
                .. (await requestRepository.GetBreakdownAsync(query.Filter, Dimension(by), top, cancellationToken))
                .Select(x => Group(x, null))
            ]
        };
        return new RequestsBreakdownDto(query.By, total, groups, asOf);
    }

    private RequestGroupDto[] ByCountry(IEnumerable<RequestBucket> clients, int top)
    {
        return
        [
            .. clients
                .GroupBy(x => countryLookup.Find(x.Key))
                .Select(x => new RequestGroupDto(
                    x.Key?.Code ?? "unknown",
                    x.Key,
                    x.Sum(y => y.Requests),
                    x.Count(),
                    x.Min(y => y.FirstSeenAt),
                    x.Max(y => y.LastSeenAt)))
                .OrderByDescending(x => x.Requests)
                .Take(top)
        ];
    }

    private static RequestGroupDto Group(RequestBucket group, CountryDto? country)
    {
        return new RequestGroupDto(group.Key, country, group.Requests, group.Clients, group.FirstSeenAt, group.LastSeenAt);
    }

    private static RequestDimension Dimension(BreakdownDimension dimension)
    {
        return dimension switch
        {
            BreakdownDimension.Host => RequestDimension.Host,
            BreakdownDimension.Path => RequestDimension.Path,
            BreakdownDimension.Method => RequestDimension.Method,
            BreakdownDimension.UserAgent => RequestDimension.UserAgent,
            BreakdownDimension.Outcome => RequestDimension.Outcome,
            BreakdownDimension.Status => RequestDimension.Status,
            BreakdownDimension.Hour => RequestDimension.Hour,
            BreakdownDimension.Day => RequestDimension.Day,
            _ => RequestDimension.Client
        };
    }
}