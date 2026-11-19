using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;

namespace Honeyslop.Data.Repositories;

public interface IAbuseReportRepository
{
    Task<bool> TryAddAsync(AbuseReport report, CancellationToken cancellationToken);
    Task<AbuseReport?> GetByIdOrDefaultAsync(long id, CancellationToken cancellationToken);
    Task<AbuseReport[]> GetByClientAsync(string clientIp, CancellationToken cancellationToken);
    Task<AbuseReport[]> GetPageAsync(ReportOrder order, SortDirection direction, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<int> CountAsync(CancellationToken cancellationToken);
    Task<Dictionary<AbuseReportState, int>> CountByStateAsync(CancellationToken cancellationToken);

    Task<AbuseReport?> TryClaimNextAsync(
        string claimedBy,
        DateTimeOffset now,
        DateTimeOffset staleBefore,
        int maxAttempts,
        CancellationToken cancellationToken);

    Task<bool> HeartbeatAsync(long id, string claimedBy, DateTimeOffset now, CancellationToken cancellationToken);

    Task<bool> CompleteAsync(
        long id,
        string claimedBy,
        string messageId,
        string sentFrom,
        int attachedRequests,
        long lastRequestId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task RetryAsync(
        long id,
        string claimedBy,
        string error,
        DateTimeOffset retryAt,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task FailAsync(long id, string claimedBy, string error, DateTimeOffset now, CancellationToken cancellationToken);
    Task<int> FailExhaustedAsync(int maxAttempts, DateTimeOffset staleBefore, DateTimeOffset now, CancellationToken cancellationToken);
}