using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;

namespace Honeyslop.Data.Repositories;

public interface IEntryRepository
{
    Task<Entry?> GetByKeyHashOrDefaultAsync(byte[] keyHash, CancellationToken cancellationToken);
    Task<Entry?> GetByIdOrDefaultAsync(long id, CancellationToken cancellationToken);
    Task<bool> TryAddAsync(Entry entry, CancellationToken cancellationToken);

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
    Task<EntryProgress[]> GetGeneratingAsync(int limit, CancellationToken cancellationToken);
    Task<EntryProgress[]> GetQueuedAsync(int limit, CancellationToken cancellationToken);

    Task<bool> HeartbeatAsync(
        long id,
        string claimedBy,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<bool> CompleteAsync(
        long id,
        string claimedBy,
        EntryResponse response,
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

    Task<EntrySummary[]> GetPageAsync(
        EntryFilter filter,
        EntrySort sort,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> CountAsync(EntryFilter filter, CancellationToken cancellationToken);
    Task<EntryStatistics> GetStatisticsAsync(CancellationToken cancellationToken);
    Task<QueueStatistics> GetQueueStatisticsAsync(CancellationToken cancellationToken);
    Task<GeneratorStatistics[]> GetGeneratorStatisticsAsync(DateTimeOffset since, CancellationToken cancellationToken);
    Task<int> DeleteAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken);
    Task<int> DeleteAsync(EntryFilter filter, CancellationToken cancellationToken);
    Task<int> DeleteUnusedSinceAsync(DateTimeOffset threshold, CancellationToken cancellationToken);
    Task<int> DeleteGeneratedBeforeAsync(DateTimeOffset threshold, CancellationToken cancellationToken);
}
