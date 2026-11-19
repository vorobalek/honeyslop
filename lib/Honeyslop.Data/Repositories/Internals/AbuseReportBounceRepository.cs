using Honeyslop.Common;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class AbuseReportBounceRepository(ApplicationDbContext dbContext) : IAbuseReportBounceRepository
{
    public async Task<int> AddAsync(IReadOnlyCollection<AbuseReportBounce> bounces, CancellationToken cancellationToken)
    {
        foreach (var bounce in bounces)
        {
            bounce.Recipient = bounce.Recipient.Truncate(BlockedAbuseContact.ContactMaxLength);
            bounce.NotificationId = bounce.NotificationId.Truncate(AbuseReportBounce.NotificationIdMaxLength);
            bounce.Status = bounce.Status?.Truncate(AbuseReportBounce.StatusMaxLength);
            bounce.Diagnostic = bounce.Diagnostic?.Truncate(AbuseReportBounce.DiagnosticMaxLength);
            bounce.Mta = bounce.Mta?.Truncate(AbuseReportBounce.MtaMaxLength);
        }

        string[] notifications = [.. bounces.Select(x => x.NotificationId).Distinct()];
        var existing = await dbContext
            .AbuseReportBounces
            .Where(x => notifications.Contains(x.NotificationId))
            .Select(x => new
            {
                x.NotificationId,
                x.Recipient
            })
            .ToArrayAsync(cancellationToken);
        AbuseReportBounce[] missing =
        [
            .. bounces
                .DistinctBy(x => (x.NotificationId, x.Recipient))
                .Where(x => !existing.Any(y => y.NotificationId == x.NotificationId && y.Recipient == x.Recipient))
        ];
        if (missing.Length == 0)
            return 0;

        await dbContext.AbuseReportBounces.AddRangeAsync(missing, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return missing.Length;
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation)
        {
            dbContext.ChangeTracker.Clear();
            return await AddOneByOneAsync(missing, cancellationToken);
        }
    }

    public Task<AbuseReportBounce[]> GetByReportAsync(long reportId, CancellationToken cancellationToken)
    {
        return dbContext
            .AbuseReportBounces
            .AsNoTracking()
            .Where(x => x.ReportId == reportId)
            .OrderBy(x => x.BouncedAt)
            .ThenBy(x => x.Id)
            .ToArrayAsync(cancellationToken);
    }

    public Task<BouncedContact[]> GetContactsAsync(IReadOnlyCollection<string> recipients, CancellationToken cancellationToken)
    {
        return Summarize(dbContext.AbuseReportBounces.Where(x => recipients.Contains(x.Recipient)))
            .ToArrayAsync(cancellationToken);
    }

    public Task<BouncedContact[]> GetContactsPageAsync(DateTimeOffset since, string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        return Summarize(Matching(since, search))
            .OrderByDescending(x => x.LastBouncedAt)
            .ThenBy(x => x.Recipient)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountContactsAsync(DateTimeOffset since, string? search, CancellationToken cancellationToken)
    {
        return Matching(since, search)
            .Select(x => x.Recipient)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    private async Task<int> AddOneByOneAsync(IReadOnlyCollection<AbuseReportBounce> bounces, CancellationToken cancellationToken)
    {
        var added = 0;
        foreach (var bounce in bounces)
        {
            await dbContext.AbuseReportBounces.AddAsync(bounce, cancellationToken);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                added++;
            }
            catch (DbUpdateException exception) when (exception.IsUniqueViolation)
            {
                dbContext.Entry(bounce).State = EntityState.Detached;
            }
        }

        return added;
    }

    private IQueryable<AbuseReportBounce> Matching(DateTimeOffset since, string? search)
    {
        var query = dbContext.AbuseReportBounces.Where(x => x.BouncedAt >= since);
        if (string.IsNullOrWhiteSpace(search))
            return query;
        var pattern = search.Trim().ContainsPattern;
        return query.Where(x =>
            EF.Functions.ILike(x.Recipient, pattern, LikePatterns.Escape) ||
            (x.Diagnostic != null && EF.Functions.ILike(x.Diagnostic, pattern, LikePatterns.Escape)));
    }

    private static IQueryable<BouncedContact> Summarize(IQueryable<AbuseReportBounce> bounces)
    {
        return bounces
            .GroupBy(x => x.Recipient)
            .Select(x => new BouncedContact
            {
                Recipient = x.Key,
                Bounces = x.Count(),
                Reports = x.Select(y => y.ReportId).Distinct().Count(),
                LastBouncedAt = x.Max(y => y.BouncedAt),
                LastReportId = x.OrderByDescending(y => y.BouncedAt).ThenByDescending(y => y.Id).Select(y => y.ReportId).First(),
                LastStatus = x.OrderByDescending(y => y.BouncedAt).ThenByDescending(y => y.Id).Select(y => y.Status).First(),
                LastDiagnostic = x.OrderByDescending(y => y.BouncedAt).ThenByDescending(y => y.Id).Select(y => y.Diagnostic).First()
            });
    }
}