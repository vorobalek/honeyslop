using System.Linq.Expressions;
using Honeyslop.Common;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class EntryRepository(ApplicationDbContext dbContext) : IEntryRepository
{
    private const int ClaimCandidates = 16;

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
        entry.RequestHeaders = entry.RequestHeaders?.Truncate(Entry.RequestHeadersMaxLength);
        entry.RequestBody = entry.RequestBody?.Truncate(Entry.RequestBodyMaxLength);
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
        Expression<Func<Entry, bool>> claimable = x =>
            (x.Status == EntryStatus.Pending && (x.ClaimedAt == null || x.ClaimedAt < staleBefore)) ||
            (x.Status == EntryStatus.Failed && x.Attempts < maxAttempts && x.UpdatedAt < retryBefore);
        var candidates = await dbContext
            .Entries
            .Where(claimable)
            .OrderByDescending(x => (x.RequestedAt ?? x.CreatedAt) >= waitingSince)
            .ThenByDescending(x => (x.RequestedAt ?? x.CreatedAt) >= waitingSince ? x.RequestedAt ?? x.CreatedAt : (DateTimeOffset?)null)
            .ThenBy(x => x.CreatedAt)
            .Select(x => x.Id)
            .Take(ClaimCandidates)
            .ToArrayAsync(cancellationToken);

        foreach (var id in candidates)
        {
            var claimed = await dbContext
                .Entries
                .Where(x => x.Id == id)
                .Where(claimable)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.Status, EntryStatus.Pending)
                        .SetProperty(x => x.ClaimedBy, claimedBy)
                        .SetProperty(x => x.ClaimedAt, now)
                        .SetProperty(x => x.UpdatedAt, now)
                        .SetProperty(x => x.Generator, (string?)null),
                    cancellationToken);
            if (claimed == 1)
                return id;
        }

        return null;
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

    public Task<Entry[]> GetGeneratingAsync(int limit, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .Where(x => x.Status == EntryStatus.Pending && x.ClaimedBy != null)
            .OrderBy(x => x.ClaimedAt)
            .Take(limit)

            .ToArrayAsync(cancellationToken);
    }

    public Task<Entry[]> GetQueuedAsync(int limit, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .Where(x => x.Status == EntryStatus.Pending && x.ClaimedBy == null)
            .OrderByDescending(x => x.RequestedAt ?? x.CreatedAt)
            .Take(limit)

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

    public Task NotifyOutputAsync(string payload, CancellationToken cancellationToken)
    {
        return dbContext
            .Database
            .ExecuteSqlRawAsync(
                "select pg_notify(@channel, @payload)",
                [new NpgsqlParameter("channel", "honeyslop_outputs"), new NpgsqlParameter("payload", payload)],
                cancellationToken);
    }

    public async Task<bool> CompleteAsync(
        Entry completed,
        string claimedBy,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var entry = await dbContext
            .Entries
            .SingleOrDefaultAsync(
                x => x.Id == completed.Id && x.Status == EntryStatus.Pending && x.ClaimedBy == claimedBy,
                cancellationToken);
        if (entry is null)
            return false;

        entry.Status = EntryStatus.Ready;
        entry.Error = null;
        entry.ResponseStatus = completed.ResponseStatus;
        entry.ResponseHeaders =
        [
            .. completed.ResponseHeaders
                .Take(Entry.ResponseHeadersMaxCount)
                .Select(x => new ResponseHeader
                {
                    Name = x.Name.Truncate(ResponseHeader.NameMaxLength),
                    Value = x.Value.Truncate(ResponseHeader.ValueMaxLength)
                })
        ];
        entry.ResponseBody = completed.ResponseBody?.Truncate(Entry.ResponseBodyMaxLength);
        entry.Model = completed.Model?.Truncate(Entry.ModelMaxLength);
        entry.Generator = completed.Generator?.Truncate(Entry.GeneratorMaxLength);
        entry.PromptTokens = completed.PromptTokens;
        entry.CompletionTokens = completed.CompletionTokens;
        entry.GenerationMs = completed.GenerationMs;
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

    public Task<Entry[]> GetPageAsync(
        string? host,
        string? target,
        EntryState? state,
        EntryOrder order,
        SortDirection direction,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext
            .Entries
            .AsNoTracking()
            .FilterBy(host, target, state);
        query = order switch
        {
            EntryOrder.Status => query.SortBy(x => x.Status, direction).ThenByDescending(x => x.Id),
            EntryOrder.Method => query.SortBy(x => x.Method, direction).ThenByDescending(x => x.Id),
            EntryOrder.Host => query.SortBy(x => x.Host, direction).ThenByDescending(x => x.Id),
            EntryOrder.Target => query.SortBy(x => x.Target, direction).ThenByDescending(x => x.Id),
            EntryOrder.ResponseStatus => query.SortBy(x => x.ResponseStatus ?? 0, direction).ThenByDescending(x => x.Id),
            EntryOrder.ResponseSize => query.SortBy(x => x.ResponseBody == null ? 0 : x.ResponseBody.Length, direction).ThenByDescending(x => x.Id),
            EntryOrder.Hits => query.SortBy(x => x.Hits, direction).ThenByDescending(x => x.Id),
            EntryOrder.LastHitAt => query.SortBy(x => x.LastHitAt ?? x.CreatedAt, direction).ThenByDescending(x => x.Id),
            _ => query.SortBy(x => x.Id, direction)
        };
        return query
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? host, string? target, EntryState? state, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .FilterBy(host, target, state)
            .CountAsync(cancellationToken);
    }

    public async Task<EntryTotals> GetTotalsAsync(CancellationToken cancellationToken)
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

        return new EntryTotals
        {
            Total = groups.Where(x => x.Status != EntryStatus.Expired).Sum(x => x.Count),
            Ready = CountOf(EntryStatus.Ready),
            Queued = CountOf(EntryStatus.Pending, false),
            Generating = CountOf(EntryStatus.Pending, true),
            Failed = CountOf(EntryStatus.Failed),
            Expired = CountOf(EntryStatus.Expired),
            Hits = groups.Where(x => x.Status != EntryStatus.Expired).Sum(x => x.Hits)
        };
    }

    public Task<DateTimeOffset?> GetOldestQueuedAtAsync(CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .Where(x => x.Status == EntryStatus.Pending && x.ClaimedBy == null)
            .MinAsync(x => (DateTimeOffset?)x.CreatedAt, cancellationToken);
    }

    public Task<GeneratorActivity[]> GetGeneratorActivityAsync(DateTimeOffset since, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .Where(x =>
                x.Generator != null &&
                x.GeneratedAt >= since)
            .GroupBy(x => x.Generator!)
            .Select(x => new GeneratorActivity
            {
                Generator = x.Key,
                Generated = x.Count(),
                AverageMs = x.Average(y => (double?)y.GenerationMs) ?? 0,
                AverageTokens = x.Average(y => (double?)y.CompletionTokens) ?? 0,
                LastGeneratedAt = x.Max(y => y.GeneratedAt)
            })
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> ExpireAsync(IReadOnlyCollection<long> ids, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .Where(x => ids.Contains(x.Id))
            .ExpireAsync(now, cancellationToken);
    }

    public Task<int> ExpireMatchingAsync(string? host, string? target, EntryState? state, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return dbContext
            .Entries
            .FilterBy(host, target, state)
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
