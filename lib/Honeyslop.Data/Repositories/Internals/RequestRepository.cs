using System.Globalization;
using System.Linq.Expressions;
using Honeyslop.Common;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class RequestRepository(ApplicationDbContext dbContext) : IRequestRepository
{
    public async Task AddRangeAsync(IReadOnlyCollection<RequestRecord> records, CancellationToken cancellationToken)
    {
        await dbContext.Requests.AddRangeAsync(records.Select(Fit), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    public async Task UpdateRangeAsync(IReadOnlyCollection<RequestRecord> records, CancellationToken cancellationToken)
    {
        dbContext.Requests.UpdateRange(records.Select(Fit));
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    public Task<RequestRecord?> GetByIdOrDefaultAsync(long id, CancellationToken cancellationToken)
    {
        return dbContext
            .Requests
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public Task<RequestRecord[]> GetPageAsync(
        string? clientIp,
        string? host,
        string? target,
        RequestOutcome? outcome,
        long? entryId,
        string? method,
        string? userAgent,
        DateTimeOffset? since,
        DateTimeOffset? until,
        RequestOrder order,
        SortDirection direction,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext
            .Requests
            .AsNoTracking()
            .FilterBy(clientIp, host, target, outcome, entryId, null, method, userAgent, since, until);
        query = order switch
        {
            RequestOrder.ClientIp => query.SortBy(x => x.ClientIp, direction).ThenByDescending(x => x.Id),
            RequestOrder.Method => query.SortBy(x => x.Method, direction).ThenByDescending(x => x.Id),
            RequestOrder.Host => query.SortBy(x => x.Host, direction).ThenByDescending(x => x.Id),
            RequestOrder.Target => query.SortBy(x => x.Target, direction).ThenByDescending(x => x.Id),
            RequestOrder.Outcome => query.SortBy(x => x.Outcome, direction).ThenByDescending(x => x.Id),
            RequestOrder.ResponseStatus => query.SortBy(x => x.ResponseStatus, direction).ThenByDescending(x => x.Id),
            RequestOrder.DurationMs => query.SortBy(x => x.DurationMs, direction).ThenByDescending(x => x.Id),
            RequestOrder.UserAgent => query.SortBy(x => x.UserAgent ?? string.Empty, direction).ThenByDescending(x => x.Id),
            _ => query.SortBy(x => x.Id, direction)
        };
        return query
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountAsync(
        string? clientIp,
        string? host,
        string? target,
        RequestOutcome? outcome,
        long? entryId,
        string? method,
        string? userAgent,
        DateTimeOffset? since,
        DateTimeOffset? until,
        long? afterId,
        CancellationToken cancellationToken)
    {
        return dbContext
            .Requests
            .FilterBy(clientIp, host, target, outcome, entryId, afterId, method, userAgent, since, until)
            .CountAsync(cancellationToken);
    }

    public Task<RequestBucket[]> GetBreakdownAsync(
        string? clientIp,
        string? host,
        string? target,
        RequestOutcome? outcome,
        long? entryId,
        string? method,
        string? userAgent,
        DateTimeOffset? since,
        DateTimeOffset? until,
        RequestDimension dimension,
        int top,
        CancellationToken cancellationToken)
    {
        var query = dbContext
            .Requests
            .AsNoTracking()
            .FilterBy(clientIp, host, target, outcome, entryId, null, method, userAgent, since, until);
        return dimension switch
        {
            RequestDimension.Client => ByCountAsync(query, x => x.ClientIp, x => x, top, cancellationToken),
            RequestDimension.Host => ByCountAsync(query, x => x.Host, x => x, top, cancellationToken),
            RequestDimension.Path => ByCountAsync(query, x => x.Target, x => x, top, cancellationToken),
            RequestDimension.Method => ByCountAsync(query, x => x.Method, x => x, top, cancellationToken),
            RequestDimension.UserAgent => ByCountAsync(query, x => x.UserAgent ?? string.Empty, x => x, top, cancellationToken),
            RequestDimension.Outcome => ByCountAsync(query, x => x.Outcome, x => x.ToString().ToLowerInvariant(), top, cancellationToken),
            RequestDimension.Status => ByCountAsync(query, x => x.ResponseStatus, x => x.ToString(CultureInfo.InvariantCulture), top, cancellationToken),
            RequestDimension.Hour => ByTimeAsync(
                query,
                x => new
                {
                    x.ReceivedAt.Year,
                    x.ReceivedAt.Month,
                    x.ReceivedAt.Day,
                    x.ReceivedAt.Hour
                },
                x => new DateTimeOffset(x.Year, x.Month, x.Day, x.Hour, 0, 0, TimeSpan.Zero),
                top,
                cancellationToken),
            _ => ByTimeAsync(
                query,
                x => new
                {
                    x.ReceivedAt.Year,
                    x.ReceivedAt.Month,
                    x.ReceivedAt.Day
                },
                x => new DateTimeOffset(x.Year, x.Month, x.Day, 0, 0, 0, TimeSpan.Zero),
                top,
                cancellationToken)
        };
    }

    public Task<ClientActivity[]> GetClientsAsync(
        DateTimeOffset since,
        ClientOrder order,
        SortDirection direction,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = Summarize(dbContext.Requests.Where(x => x.ReceivedAt >= since));
        query = order switch
        {
            ClientOrder.ClientIp => query.SortBy(x => x.ClientIp, direction),
            ClientOrder.Requests => query.SortBy(x => x.Requests, direction).ThenByDescending(x => x.LastSeenAt),
            ClientOrder.Hosts => query.SortBy(x => x.Hosts, direction).ThenByDescending(x => x.LastSeenAt),
            ClientOrder.Targets => query.SortBy(x => x.Targets, direction).ThenByDescending(x => x.LastSeenAt),
            ClientOrder.FirstSeenAt => query.SortBy(x => x.FirstSeenAt, direction),
            ClientOrder.LastUserAgent => query.SortBy(x => x.LastUserAgent ?? string.Empty, direction).ThenByDescending(x => x.LastSeenAt),
            _ => query.SortBy(x => x.LastSeenAt, direction)
        };
        return query
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountClientsAsync(DateTimeOffset since, CancellationToken cancellationToken)
    {
        return dbContext
            .Requests
            .Where(x => x.ReceivedAt >= since)
            .Select(x => x.ClientIp)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public Task<ClientActivity?> GetClientOrDefaultAsync(string clientIp, CancellationToken cancellationToken)
    {
        return Summarize(dbContext.Requests.Where(x => x.ClientIp == clientIp))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<long?> GetLogStartAsync(string clientIp, int latest, CancellationToken cancellationToken)
    {
        var requests = dbContext.Requests.Where(x => x.ClientIp == clientIp);
        return await requests
                   .OrderByDescending(x => x.Id)
                   .Skip(latest - 1)
                   .Select(x => (long?)x.Id)
                   .FirstOrDefaultAsync(cancellationToken) ??
               await requests.MinAsync(x => (long?)x.Id, cancellationToken);
    }

    public Task<RequestRecord[]> GetLogAsync(string clientIp, long fromId, int limit, CancellationToken cancellationToken)
    {
        return dbContext
            .Requests
            .AsNoTracking()
            .Where(x => x.ClientIp == clientIp && x.Id >= fromId)
            .OrderBy(x => x.Id)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
    }

    public Task<RequestRecord[]> GetTimingsAsync(IReadOnlyCollection<string> clientIps, long afterId, int limit, CancellationToken cancellationToken)
    {
        return dbContext
            .Requests
            .AsNoTracking()
            .Where(x => clientIps.Contains(x.ClientIp) && x.Id > afterId)
            .Where(x => x.Outcome == RequestOutcome.Hit ||
                        x.Outcome == RequestOutcome.Generated ||
                        x.Outcome == RequestOutcome.Abandoned ||
                        x.Outcome == RequestOutcome.Waiting)
            .OrderBy(x => x.Id)
            .Take(limit)
            .Select(x => new RequestRecord
            {
                Id = x.Id,
                ReceivedAt = x.ReceivedAt,
                ClientIp = x.ClientIp,
                Outcome = x.Outcome,
                DurationMs = x.DurationMs
            })
            .ToArrayAsync(cancellationToken);
    }

    public Task<string[]> GetClientIpsAsync(CancellationToken cancellationToken)
    {
        return dbContext
            .Requests
            .AsNoTracking()
            .Select(x => x.ClientIp)
            .Distinct()
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> DeleteReceivedBeforeAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
    {
        return dbContext
            .Requests
            .Where(x => x.ReceivedAt < threshold)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static IQueryable<ClientActivity> Summarize(IQueryable<RequestRecord> requests)
    {
        return requests
            .AsNoTracking()
            .GroupBy(x => x.ClientIp)
            .Select(x => new ClientActivity
            {
                ClientIp = x.Key,
                Requests = x.Count(),
                Hosts = x.Select(y => y.Host).Distinct().Count(),
                Targets = x.Select(y => y.Target).Distinct().Count(),
                FirstSeenAt = x.Min(y => y.ReceivedAt),
                LastSeenAt = x.Max(y => y.ReceivedAt),
                LastUserAgent = x.OrderByDescending(y => y.Id).Select(y => y.UserAgent).FirstOrDefault()
            });
    }

    private static async Task<RequestBucket[]> ByCountAsync<TKey>(
        IQueryable<RequestRecord> query,
        Expression<Func<RequestRecord, TKey>> key,
        Func<TKey, string> format,
        int top,
        CancellationToken cancellationToken)
    {
        var groups = await query
            .GroupBy(key)
            .Select(x => new
            {
                x.Key,
                Requests = x.Count(),
                Clients = x.Select(y => y.ClientIp).Distinct().Count(),
                FirstSeenAt = x.Min(y => y.ReceivedAt),
                LastSeenAt = x.Max(y => y.ReceivedAt)
            })
            .OrderByDescending(x => x.Requests)
            .ThenByDescending(x => x.LastSeenAt)
            .Take(top)
            .ToArrayAsync(cancellationToken);
        return
        [
            .. groups.Select(x => new RequestBucket
            {
                Key = format(x.Key),
                Requests = x.Requests,
                Clients = x.Clients,
                FirstSeenAt = x.FirstSeenAt,
                LastSeenAt = x.LastSeenAt
            })
        ];
    }

    private static async Task<RequestBucket[]> ByTimeAsync<TKey>(
        IQueryable<RequestRecord> query,
        Expression<Func<RequestRecord, TKey>> key,
        Func<TKey, DateTimeOffset> start,
        int top,
        CancellationToken cancellationToken)
    {
        var groups = await query
            .GroupBy(key)
            .Select(x => new
            {
                x.Key,
                Requests = x.Count(),
                Clients = x.Select(y => y.ClientIp).Distinct().Count(),
                FirstSeenAt = x.Min(y => y.ReceivedAt),
                LastSeenAt = x.Max(y => y.ReceivedAt)
            })
            .OrderByDescending(x => x.LastSeenAt)
            .Take(top)
            .ToArrayAsync(cancellationToken);
        return
        [
            .. groups
                .OrderBy(x => x.FirstSeenAt)
                .Select(x => new RequestBucket
                {
                    Key = start(x.Key).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    Requests = x.Requests,
                    Clients = x.Clients,
                    FirstSeenAt = x.FirstSeenAt,
                    LastSeenAt = x.LastSeenAt
                })
        ];
    }

    private static RequestRecord Fit(RequestRecord record)
    {
        record.Headers = record.Headers?.Truncate(RequestRecord.HeadersMaxLength);
        record.Body = record.Body?.Truncate(RequestRecord.BodyMaxLength);
        return record;
    }
}