using System.Linq.Expressions;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class RequestRepository(ApplicationDbContext dbContext) : IRequestRepository
{
    private static readonly Expression<Func<ClientRow, ClientSummary>> ToSummary = x => new ClientSummary(
        x.ClientIp,
        x.Requests,
        x.Hosts,
        x.Targets,
        x.FirstSeenAt,
        x.LastSeenAt,
        x.LastUserAgent);

    public async Task AddRangeAsync(IReadOnlyCollection<RequestRecord> records, CancellationToken cancellationToken)
    {
        await dbContext.Requests.AddRangeAsync(records, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    public async Task UpdateRangeAsync(IReadOnlyCollection<RequestRecord> records, CancellationToken cancellationToken)
    {
        dbContext.Requests.UpdateRange(records);
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

    public Task<RequestSummary[]> GetPageAsync(
        RequestFilter filter,
        Sorting<RequestColumn>? sort,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext
            .Requests
            .AsNoTracking()
            .FilterBy(filter);
        query = sort switch
        {
            null => query.OrderByDescending(x => x.Id),
            { Column: RequestColumn.Client } s => query.SortBy(x => x.ClientIp, s.Direction).ThenByDescending(x => x.Id),
            { Column: RequestColumn.Method } s => query.SortBy(x => x.Method, s.Direction).ThenByDescending(x => x.Id),
            { Column: RequestColumn.Host } s => query.SortBy(x => x.Host, s.Direction).ThenByDescending(x => x.Id),
            { Column: RequestColumn.Path } s => query.SortBy(x => x.Target, s.Direction).ThenByDescending(x => x.Id),
            { Column: RequestColumn.Outcome } s => query.SortBy(x => x.Outcome, s.Direction).ThenByDescending(x => x.Id),
            { Column: RequestColumn.Code } s => query.SortBy(x => x.ResponseStatus, s.Direction).ThenByDescending(x => x.Id),
            { Column: RequestColumn.Duration } s => query.SortBy(x => x.DurationMs, s.Direction).ThenByDescending(x => x.Id),
            { Column: RequestColumn.UserAgent } s => query.SortBy(x => x.UserAgent ?? string.Empty, s.Direction).ThenByDescending(x => x.Id),
            { Direction: var direction } => query.SortBy(x => x.Id, direction)
        };
        return query
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .Select(x => new RequestSummary(
                x.Id,
                x.ReceivedAt,
                x.ClientIp,
                x.Method,
                x.Host,
                x.Target,
                x.UserAgent,
                x.Outcome,
                x.ResponseStatus,
                x.EntryId,
                x.DurationMs))
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountAsync(RequestFilter filter, CancellationToken cancellationToken)
    {
        return dbContext
            .Requests
            .FilterBy(filter)
            .CountAsync(cancellationToken);
    }

    public Task<ClientSummary[]> GetClientsAsync(
        DateTimeOffset since,
        Sorting<ClientColumn>? sort,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = Summarize(dbContext.Requests.Where(x => x.ReceivedAt >= since));
        query = sort switch
        {
            null => query.OrderByDescending(x => x.LastSeenAt),
            { Column: ClientColumn.Client } s => query.SortBy(x => x.ClientIp, s.Direction),
            { Column: ClientColumn.Requests } s => query.SortBy(x => x.Requests, s.Direction).ThenByDescending(x => x.LastSeenAt),
            { Column: ClientColumn.Hosts } s => query.SortBy(x => x.Hosts, s.Direction).ThenByDescending(x => x.LastSeenAt),
            { Column: ClientColumn.Paths } s => query.SortBy(x => x.Targets, s.Direction).ThenByDescending(x => x.LastSeenAt),
            { Column: ClientColumn.FirstSeen } s => query.SortBy(x => x.FirstSeenAt, s.Direction),
            { Column: ClientColumn.UserAgent } s => query.SortBy(x => x.LastUserAgent ?? string.Empty, s.Direction).ThenByDescending(x => x.LastSeenAt),
            { Direction: var direction } => query.SortBy(x => x.LastSeenAt, direction)
        };
        return query
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .Select(ToSummary)
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

    public Task<ClientSummary?> GetClientOrDefaultAsync(string clientIp, CancellationToken cancellationToken)
    {
        return Summarize(dbContext.Requests.Where(x => x.ClientIp == clientIp))
            .Select(ToSummary)
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

    public Task<int> DeleteReceivedBeforeAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
    {
        return dbContext
            .Requests
            .Where(x => x.ReceivedAt < threshold)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static IQueryable<ClientRow> Summarize(IQueryable<RequestRecord> requests)
    {
        return requests
            .AsNoTracking()
            .GroupBy(x => x.ClientIp)
            .Select(x => new ClientRow
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

    private sealed class ClientRow
    {
        public string ClientIp { get; init; } = null!;
        public int Requests { get; init; }
        public int Hosts { get; init; }
        public int Targets { get; init; }
        public DateTimeOffset FirstSeenAt { get; init; }
        public DateTimeOffset LastSeenAt { get; init; }
        public string? LastUserAgent { get; init; }
    }
}
