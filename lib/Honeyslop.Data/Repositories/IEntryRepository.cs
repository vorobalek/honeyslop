using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;

namespace Honeyslop.Data.Repositories;

public interface IEntryRepository
{
    Task<Entry?> GetByKeyHashOrDefaultAsync(byte[] keyHash, CancellationToken cancellationToken);
    Task<Entry?> GetByIdOrDefaultAsync(long id, CancellationToken cancellationToken);
    Task<bool> TryAddAsync(Entry entry, CancellationToken cancellationToken);
    Task<Entry[]> GetChangeableAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken);
    Task<bool> TryReplaceAsync(IReadOnlyCollection<Entry> replacements, DateTimeOffset now, CancellationToken cancellationToken);
    Task<long[]> SetResponseDelayAsync(IReadOnlyCollection<long> ids, int? seconds, DateTimeOffset now, CancellationToken cancellationToken);
    Task<bool> AnyFromClientAsync(string clientIp, CancellationToken cancellationToken);

    Task<long?> TryClaimNextAsync(
        string claimedBy,
        DateTimeOffset now,
        DateTimeOffset waitingSince,
        DateTimeOffset staleBefore,
        DateTimeOffset retryBefore,
        int maxAttempts,
        CancellationToken cancellationToken);

    Task<Entry[]> GetFinishedAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken);
    Task TouchRequestedAsync(long id, DateTimeOffset now, CancellationToken cancellationToken);
    Task<Entry[]> GetGeneratingAsync(int limit, CancellationToken cancellationToken);
    Task<Entry[]> GetQueuedAsync(int limit, CancellationToken cancellationToken);

    Task<bool> HeartbeatAsync(
        long id,
        string claimedBy,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task MarkDispatchedAsync(
        long id,
        string claimedBy,
        string generator,
        CancellationToken cancellationToken);

    Task NotifyOutputAsync(string payload, CancellationToken cancellationToken);

    Task<bool> CompleteAsync(
        Entry completed,
        string claimedBy,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task RequeueAsync(
        long id,
        string claimedBy,
        string reason,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task FailAsync(
        long id,
        string claimedBy,
        string error,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task AddHitsAsync(
        long id,
        long hits,
        DateTimeOffset lastHitAt,
        CancellationToken cancellationToken);

    Task<Entry[]> GetPageAsync(
        string? host,
        string? target,
        EntryState? state,
        EntryOrder order,
        SortDirection direction,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> CountAsync(string? host, string? target, EntryState? state, CancellationToken cancellationToken);
    Task<EntryTotals> GetTotalsAsync(CancellationToken cancellationToken);
    Task<DateTimeOffset?> GetOldestQueuedAtAsync(CancellationToken cancellationToken);
    Task<GeneratorActivity[]> GetGeneratorActivityAsync(DateTimeOffset since, CancellationToken cancellationToken);
    Task<int> ExpireAsync(IReadOnlyCollection<long> ids, DateTimeOffset now, CancellationToken cancellationToken);
    Task<int> ExpireMatchingAsync(string? host, string? target, EntryState? state, DateTimeOffset now, CancellationToken cancellationToken);
    Task<int> ExpireUnusedSinceAsync(DateTimeOffset threshold, DateTimeOffset now, CancellationToken cancellationToken);
    Task<int> ExpireGeneratedBeforeAsync(DateTimeOffset threshold, DateTimeOffset now, CancellationToken cancellationToken);
    Task<int> DeleteExpiredUnreferencedAsync(DateTimeOffset expiredBefore, CancellationToken cancellationToken);
}