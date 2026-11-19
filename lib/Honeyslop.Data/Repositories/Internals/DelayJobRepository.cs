using System.Linq.Expressions;
using Honeyslop.Common;
using Honeyslop.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class DelayJobRepository(ApplicationDbContext dbContext) : IDelayJobRepository
{
    private const int ClaimCandidates = 8;

    public async Task AddRangeAsync(IReadOnlyCollection<DelayJob> jobs, CancellationToken cancellationToken)
    {
        if (jobs.Count == 0)
            return;
        await dbContext.DelayJobs.AddRangeAsync(jobs, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return dbContext.DelayJobs.CountAsync(cancellationToken);
    }

    public async Task<DelayJob?> TryClaimNextAsync(string claimedBy, DateTimeOffset now, DateTimeOffset staleBefore, CancellationToken cancellationToken)
    {
        var worker = claimedBy.Truncate(Entry.ClaimedByMaxLength);
        Expression<Func<DelayJob, bool>> claimable = x => x.ClaimedAt == null || x.ClaimedAt < staleBefore;
        var candidates = await dbContext
            .DelayJobs
            .Where(claimable)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .Take(ClaimCandidates)
            .ToArrayAsync(cancellationToken);

        foreach (var id in candidates)
        {
            var claimed = await dbContext
                .DelayJobs
                .Where(x => x.Id == id)
                .Where(claimable)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.ClaimedBy, worker)
                        .SetProperty(x => x.ClaimedAt, now),
                    cancellationToken);
            if (claimed > 0)
                return await dbContext.DelayJobs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.ClaimedBy == worker, cancellationToken);
        }

        return null;
    }

    public async Task<bool> CompleteAsync(long id, string claimedBy, CancellationToken cancellationToken)
    {
        var worker = claimedBy.Truncate(Entry.ClaimedByMaxLength);
        return await dbContext
            .DelayJobs
            .Where(x => x.Id == id && x.ClaimedBy == worker)
            .ExecuteDeleteAsync(cancellationToken) > 0;
    }

    public Task<int> ClearAsync(CancellationToken cancellationToken)
    {
        return dbContext.DelayJobs.ExecuteDeleteAsync(cancellationToken);
    }
}