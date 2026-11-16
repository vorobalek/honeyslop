using Honeyslop.Data.Entities;
using Honeyslop.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class RequestRepository(ApplicationDbContext dbContext) : IRequestRepository
{
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
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        return dbContext
            .Requests
            .AsNoTracking()
            .FilterBy(filter)
            .OrderByDescending(x => x.Id)
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
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var skip = pageNumber * pageSize;
        return dbContext
            .Database
            .SqlQuery<ClientSummary>(
                $"""
                 select client_ip,
                        count(*)::int as requests,
                        count(distinct host)::int as hosts,
                        count(distinct target)::int as targets,
                        min(received_at) as first_seen_at,
                        max(received_at) as last_seen_at,
                        (array_agg(user_agent order by id desc))[1] as last_user_agent
                 from public.request
                 where received_at >= {since}
                 group by client_ip
                 order by max(received_at) desc
                 offset {skip} limit {pageSize}
                 """)
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

    public Task<int> DeleteReceivedBeforeAsync(DateTimeOffset threshold, CancellationToken cancellationToken)
    {
        return dbContext
            .Requests
            .Where(x => x.ReceivedAt < threshold)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
