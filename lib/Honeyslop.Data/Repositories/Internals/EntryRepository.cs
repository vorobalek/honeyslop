using Honeyslop.Common;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class EntryRepository(ApplicationDbContext dbContext) : IEntryRepository
{
    public Task<Entry?> GetByKeyHashOrDefaultAsync(byte[] keyHash, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.KeyHash == keyHash && x.Status != EntryStatus.Expired,
                cancellationToken);
    }

    public Task<Entry?> GetByIdOrDefaultAsync(long id, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public Task<bool> AnyFromClientAsync(string clientIp, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .AnyAsync(x => x.ClientIp == clientIp, cancellationToken);
    }

    public async Task<bool> TryAddAsync(Entry entry, CancellationToken cancellationToken)
    {
        await dbContext.Entries.AddAsync(entry, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation)
        {
            dbContext.Entry(entry).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<long?> TryClaimNextAsync(
        string claimedBy,
        DateTimeOffset now,
        DateTimeOffset waitingSince,
        DateTimeOffset staleBefore,
        DateTimeOffset retryBefore,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        const int pending = (int)EntryStatus.Pending;
        const int failed = (int)EntryStatus.Failed;
        var claimed = await dbContext
            .Database
            .SqlQuery<long>(
                $"""
                 update public.entry
                 set status = {pending}, claimed_by = {claimedBy}, claimed_at = {now}, updated_at = {now}, generator = null
                 where id = (
                     select id
                     from public.entry
                     where (status = {pending} and (claimed_at is null or claimed_at < {staleBefore}))
                        or (status = {failed} and attempts < {maxAttempts} and updated_at < {retryBefore})
                     order by coalesce(requested_at, created_at) >= {waitingSince} desc,
                              case when coalesce(requested_at, created_at) >= {waitingSince} then coalesce(requested_at, created_at) end desc,
                              created_at
                     limit 1
                     for update skip locked)
                 returning id as "Value"
                 """)
            .ToListAsync(cancellationToken);
        return claimed.Count > 0 ? claimed[0] : null;
    }

    public Task TouchRequestedAsync(long id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var throttle = now.AddSeconds(-5);
        return dbContext
            .Entries
            .Where(x =>
                x.Id == id &&
                (x.Status == EntryStatus.Pending || x.Status == EntryStatus.Failed) &&
                (x.RequestedAt == null || x.RequestedAt < throttle))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.RequestedAt, now),
                cancellationToken);
    }

    public Task<EntryProgress[]> GetGeneratingAsync(int limit, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .Where(x => x.Status == EntryStatus.Pending && x.ClaimedBy != null)
            .OrderBy(x => x.ClaimedAt)
            .Take(limit)
            .Select(x => new EntryProgress(x.Id, x.Method, x.Host, x.Target, x.Attempts, x.ClaimedBy, x.ClaimedAt, x.CreatedAt, x.RequestedAt, x.Generator))
            .ToArrayAsync(cancellationToken);
    }

    public Task<EntryProgress[]> GetQueuedAsync(int limit, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .Where(x => x.Status == EntryStatus.Pending && x.ClaimedBy == null)
            .OrderByDescending(x => x.RequestedAt ?? x.CreatedAt)
            .Take(limit)
            .Select(x => new EntryProgress(x.Id, x.Method, x.Host, x.Target, x.Attempts, x.ClaimedBy, x.ClaimedAt, x.CreatedAt, x.RequestedAt, x.Generator))
            .ToArrayAsync(cancellationToken);
    }

    public Task<Entry[]> GetFinishedAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id) && x.Status != EntryStatus.Pending)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> HeartbeatAsync(
        long id,
        string claimedBy,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var updated = await dbContext
            .Entries
            .Where(x => x.Id == id && x.Status == EntryStatus.Pending && x.ClaimedBy == claimedBy)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.ClaimedAt, now),
                cancellationToken);
        return updated == 1;
    }

    public Task MarkDispatchedAsync(
        long id,
        string claimedBy,
        string generator,
        CancellationToken cancellationToken)
    {
        var truncatedGenerator = generator.Truncate(Entry.GeneratorMaxLength);
        return dbContext
            .Entries
            .Where(x => x.Id == id && x.Status == EntryStatus.Pending && x.ClaimedBy == claimedBy)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.Generator, truncatedGenerator),
                cancellationToken);
    }

    public Task NotifyOutputAsync(GenerationOutput output, CancellationToken cancellationToken)
    {
        return dbContext
            .Database
            .ExecuteSqlAsync(
                $"select pg_notify('honeyslop_outputs', {output.ToPayload()})",
                cancellationToken);
    }

    public async Task<bool> CompleteAsync(
        long id,
        string claimedBy,
        EntryResponse response,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var entry = await dbContext
            .Entries
            .SingleOrDefaultAsync(
                x => x.Id == id && x.Status == EntryStatus.Pending && x.ClaimedBy == claimedBy,
                cancellationToken);
        if (entry is null)
            return false;

        entry.Status = EntryStatus.Ready;
        entry.Error = null;
        entry.ResponseStatus = response.Status;
        entry.ResponseHeaders =
        [
            ..response.Headers.Select(x => new ResponseHeader
            {
                Name = x.Name,
                Value = x.Value
            })
        ];
        entry.ResponseBody = response.Body;
        entry.Model = response.Model?.Truncate(Entry.ModelMaxLength);
        entry.Generator = response.Generator?.Truncate(Entry.GeneratorMaxLength);
        entry.PromptTokens = response.PromptTokens;
        entry.CompletionTokens = response.CompletionTokens;
        entry.GenerationMs = response.GenerationMs;
        entry.GeneratedAt = now;
        entry.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task RequeueAsync(
        long id,
        string claimedBy,
        string reason,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var truncatedReason = reason.Truncate(Entry.ErrorMaxLength);
        return dbContext
            .Entries
            .Where(x => x.Id == id && x.Status == EntryStatus.Pending && x.ClaimedBy == claimedBy)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ClaimedBy, (string?)null)
                    .SetProperty(x => x.ClaimedAt, (DateTimeOffset?)null)
                    .SetProperty(x => x.Generator, (string?)null)
                    .SetProperty(x => x.Error, truncatedReason)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
    }

    public Task FailAsync(
        long id,
        string claimedBy,
        string error,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var truncatedError = error.Truncate(Entry.ErrorMaxLength);
        return dbContext
            .Entries
            .Where(x => x.Id == id && x.Status == EntryStatus.Pending && x.ClaimedBy == claimedBy)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, EntryStatus.Failed)
                    .SetProperty(x => x.Error, truncatedError)
                    .SetProperty(x => x.Attempts, x => x.Attempts + 1)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
    }

    public Task AddHitsAsync(
        long id,
        long hits,
        DateTimeOffset lastHitAt,
        CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Hits, x => x.Hits + hits)
                    .SetProperty(x => x.LastHitAt, lastHitAt),
                cancellationToken);
    }

    public Task<EntrySummary[]> GetPageAsync(
        EntryFilter filter,
        EntrySort sort,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext
            .Entries
            .AsNoTracking()
            .FilterBy(filter);
        query = sort switch
        {
            EntrySort.LastHit => query
                .OrderByDescending(x => x.LastHitAt ?? x.CreatedAt)
                .ThenByDescending(x => x.Id),
            EntrySort.MostHits => query
                .OrderByDescending(x => x.Hits)
                .ThenByDescending(x => x.Id),
            _ => query
                .OrderByDescending(x => x.Id)
        };
        return query
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .Select(x => new EntrySummary(
                x.Id,
                x.Status,
                x.Method,
                x.Host,
                x.Target,
                x.ResponseStatus,
                x.ResponseBody == null ? 0 : x.ResponseBody.Length,
                x.Hits,
                x.CreatedAt,
                x.LastHitAt,
                x.Error,
                x.ClaimedBy,
                x.ClaimedAt,
                x.Generator))
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountAsync(EntryFilter filter, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .FilterBy(filter)
            .CountAsync(cancellationToken);
    }

    public async Task<EntryStatistics> GetStatisticsAsync(CancellationToken cancellationToken)
    {
        var groups = await dbContext
            .Entries
            .GroupBy(x => new
            {
                x.Status,
                Claimed = x.ClaimedBy != null
            })
            .Select(x => new
            {
                x.Key.Status,
                x.Key.Claimed,
                Count = x.Count(),
                Hits = x.Sum(y => y.Hits)
            })
            .ToArrayAsync(cancellationToken);

        int CountOf(EntryStatus status, bool? claimed = null)
        {
            return groups
                .Where(x => x.Status == status && (claimed is null || x.Claimed == claimed))
                .Sum(x => x.Count);
        }

        return new EntryStatistics(
            groups.Where(x => x.Status != EntryStatus.Expired).Sum(x => x.Count),
            CountOf(EntryStatus.Ready),
            CountOf(EntryStatus.Pending, false),
            CountOf(EntryStatus.Pending, true),
            CountOf(EntryStatus.Failed),
            CountOf(EntryStatus.Expired),
            groups.Where(x => x.Status != EntryStatus.Expired).Sum(x => x.Hits));
    }

    public async Task<QueueStatistics> GetQueueStatisticsAsync(CancellationToken cancellationToken)
    {
        var pending = await dbContext
            .Entries
            .Where(x => x.Status == EntryStatus.Pending)
            .GroupBy(x => x.ClaimedBy)
            .Select(x => new
            {
                Instance = x.Key,
                Count = x.Count()
            })
            .ToArrayAsync(cancellationToken);
        var failed = await dbContext
            .Entries
            .CountAsync(x => x.Status == EntryStatus.Failed, cancellationToken);
        var oldestQueuedAt = await dbContext
            .Entries
            .Where(x => x.Status == EntryStatus.Pending && x.ClaimedBy == null)
            .MinAsync(x => (DateTimeOffset?)x.CreatedAt, cancellationToken);
        return new QueueStatistics(
            pending.Where(x => x.Instance == null).Sum(x => x.Count),
            failed,
            oldestQueuedAt,
            pending
                .Where(x => x.Instance != null)
                .ToDictionary(x => x.Instance!, x => x.Count));
    }

    public Task<GeneratorStatistics[]> GetGeneratorStatisticsAsync(DateTimeOffset since, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .Where(x =>
                x.Generator != null &&
                x.GeneratedAt >= since)
            .GroupBy(x => x.Generator!)
            .Select(x => new GeneratorStatistics(
                x.Key,
                x.Count(),
                x.Average(y => (double?)y.GenerationMs) ?? 0,
                x.Average(y => (double?)y.CompletionTokens) ?? 0,
                x.Max(y => y.GeneratedAt)))
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> ExpireAsync(IReadOnlyCollection<long> ids, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .Where(x => ids.Contains(x.Id))
            .ExpireAsync(now, cancellationToken);
    }

    public Task<int> ExpireAsync(EntryFilter filter, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .FilterBy(filter)
            .ExpireAsync(now, cancellationToken);
    }

    public Task<int> ExpireUnusedSinceAsync(DateTimeOffset threshold, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .Where(x =>
                (x.Status == EntryStatus.Ready || x.Status == EntryStatus.Failed) &&
                (x.LastHitAt ?? x.CreatedAt) < threshold)
            .ExpireAsync(now, cancellationToken);
    }

    public Task<int> ExpireGeneratedBeforeAsync(DateTimeOffset threshold, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .Where(x =>
                x.Status == EntryStatus.Ready &&
                x.GeneratedAt < threshold)
            .ExpireAsync(now, cancellationToken);
    }

    public Task<int> DeleteExpiredUnreferencedAsync(DateTimeOffset expiredBefore, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .Where(x =>
                x.Status == EntryStatus.Expired &&
                x.UpdatedAt < expiredBefore &&
                !dbContext.Requests.Any(y => y.EntryId == x.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
