using System.Linq.Expressions;
using Honeyslop.Common;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class AbuseReportRepository(ApplicationDbContext dbContext) : IAbuseReportRepository
{
    private const int ClaimCandidates = 8;

    public async Task<bool> TryAddAsync(AbuseReport report, CancellationToken cancellationToken)
    {
        await dbContext.AbuseReports.AddAsync(report, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation)
        {
            dbContext.Entry(report).State = EntityState.Detached;
            return false;
        }
    }

    public Task<AbuseReport?> GetByIdOrDefaultAsync(long id, CancellationToken cancellationToken)
    {
        return dbContext
            .AbuseReports
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<AbuseReport[]> GetByClientAsync(string clientIp, CancellationToken cancellationToken)
    {
        return dbContext
            .AbuseReports
            .AsNoTracking()
            .Where(x => x.ClientIp == clientIp)
            .OrderByDescending(x => x.Id)
            .ToArrayAsync(cancellationToken);
    }

    public Task<AbuseReport[]> GetPageAsync(Sorting<ReportColumn>? sort, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext
            .AbuseReports
            .AsNoTracking();
        query = sort switch
        {
            null => query.OrderByDescending(x => x.Id),
            { Column: ReportColumn.Client } s => query.SortBy(x => x.ClientIp, s.Direction).ThenByDescending(x => x.Id),
            { Column: ReportColumn.By } s => query.SortBy(x => x.RequestedBy, s.Direction).ThenByDescending(x => x.Id),
            { Column: ReportColumn.To } s => query.SortBy(x => x.Recipients[0], s.Direction).ThenByDescending(x => x.Id),
            { Column: ReportColumn.Status } s => query.SortBy(x => x.Status, s.Direction).ThenByDescending(x => x.Id),
            { Column: ReportColumn.Sent } s => query.SortBy(x => x.SentAt ?? x.CreatedAt, s.Direction).ThenByDescending(x => x.Id),
            { Column: ReportColumn.Requests } s => query.SortBy(x => x.AttachedRequests ?? 0, s.Direction).ThenByDescending(x => x.Id),
            { Column: ReportColumn.Attempts } s => query.SortBy(x => x.Attempts, s.Direction).ThenByDescending(x => x.Id),
            { Column: ReportColumn.Error } s => query.SortBy(x => x.Error ?? string.Empty, s.Direction).ThenByDescending(x => x.Id),
            { Direction: var direction } => query.SortBy(x => x.Id, direction)
        };
        return query
            .Skip(pageNumber * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return dbContext
            .AbuseReports
            .CountAsync(cancellationToken);
    }

    public async Task<Dictionary<AbuseReportState, int>> CountByStateAsync(CancellationToken cancellationToken)
    {
        var counts = await dbContext
            .AbuseReports
            .GroupBy(x =>
                x.Status == AbuseReportStatus.Sent ? AbuseReportState.Sent :
                x.Status == AbuseReportStatus.Failed ? AbuseReportState.Failed :
                x.ClaimedBy != null ? AbuseReportState.Sending :
                x.RetryAt != null ? AbuseReportState.Retrying :
                AbuseReportState.Queued)
            .Select(x => new { State = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);
        return Enum
            .GetValues<AbuseReportState>()
            .ToDictionary(x => x, x => counts.FirstOrDefault(y => y.State == x)?.Count ?? 0);
    }

    public async Task<AbuseReport?> TryClaimNextAsync(
        string claimedBy,
        DateTimeOffset now,
        DateTimeOffset staleBefore,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        Expression<Func<AbuseReport, bool>> claimable = x =>
            x.Status == AbuseReportStatus.Pending &&
            x.Attempts < maxAttempts &&
            (x.ClaimedAt == null || x.ClaimedAt < staleBefore) &&
            (x.RetryAt == null || x.RetryAt <= now);
        var candidates = await dbContext
            .AbuseReports
            .Where(claimable)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .Take(ClaimCandidates)
            .ToArrayAsync(cancellationToken);

        foreach (var id in candidates)
        {
            var claimed = await dbContext
                .AbuseReports
                .Where(x => x.Id == id)
                .Where(claimable)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.ClaimedBy, claimedBy)
                        .SetProperty(x => x.ClaimedAt, now)
                        .SetProperty(x => x.Attempts, x => x.Attempts + 1)
                        .SetProperty(x => x.UpdatedAt, now),
                    cancellationToken);
            if (claimed == 1)
                return await dbContext
                    .AbuseReports
                    .AsNoTracking()
                    .SingleAsync(x => x.Id == id, cancellationToken);
        }

        return null;
    }

    public async Task<bool> HeartbeatAsync(long id, string claimedBy, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var updated = await Claimed(id, claimedBy)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.ClaimedAt, now),
                cancellationToken);
        return updated == 1;
    }

    public async Task<bool> CompleteAsync(
        long id,
        string claimedBy,
        string messageId,
        string sentFrom,
        int attachedRequests,
        long lastRequestId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var truncatedMessageId = messageId.Truncate(AbuseReport.MessageIdMaxLength);
        var truncatedSentFrom = sentFrom.Truncate(AbuseReport.SentFromMaxLength);
        var updated = await Claimed(id, claimedBy)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, AbuseReportStatus.Sent)
                    .SetProperty(x => x.MessageId, truncatedMessageId)
                    .SetProperty(x => x.SentFrom, truncatedSentFrom)
                    .SetProperty(x => x.AttachedRequests, attachedRequests)
                    .SetProperty(x => x.LastRequestId, lastRequestId)
                    .SetProperty(x => x.Error, (string?)null)
                    .SetProperty(x => x.SentAt, now)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
        return updated == 1;
    }

    public Task RetryAsync(
        long id,
        string claimedBy,
        string error,
        DateTimeOffset retryAt,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var truncatedError = error.Truncate(AbuseReport.ErrorMaxLength);
        return Claimed(id, claimedBy)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ClaimedBy, (string?)null)
                    .SetProperty(x => x.ClaimedAt, (DateTimeOffset?)null)
                    .SetProperty(x => x.RetryAt, retryAt)
                    .SetProperty(x => x.Error, truncatedError)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
    }

    public Task FailAsync(long id, string claimedBy, string error, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var truncatedError = error.Truncate(AbuseReport.ErrorMaxLength);
        return Claimed(id, claimedBy)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, AbuseReportStatus.Failed)
                    .SetProperty(x => x.Error, truncatedError)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
    }

    public Task<int> FailExhaustedAsync(
        int maxAttempts,
        DateTimeOffset staleBefore,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var abandoned = $"Gave up after {maxAttempts} attempts: the reporter stopped while sending";
        return dbContext
            .AbuseReports
            .Where(x =>
                x.Status == AbuseReportStatus.Pending &&
                x.Attempts >= maxAttempts &&
                (x.ClaimedAt == null || x.ClaimedAt < staleBefore))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, AbuseReportStatus.Failed)
                    .SetProperty(x => x.Error, x => x.Error ?? abandoned)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
    }

    private IQueryable<AbuseReport> Claimed(long id, string claimedBy)
    {
        return dbContext
            .AbuseReports
            .Where(x => x.Id == id && x.Status == AbuseReportStatus.Pending && x.ClaimedBy == claimedBy);
    }
}
