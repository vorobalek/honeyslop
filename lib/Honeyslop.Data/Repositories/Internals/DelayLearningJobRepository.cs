using System.Linq.Expressions;
using Honeyslop.Common;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Extensions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class DelayLearningJobRepository(ApplicationDbContext dbContext) : IDelayLearningJobRepository
{
    private const int ClaimCandidates = 8;

    public async Task EnqueueAsync(IReadOnlyCollection<string> clientIps, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (clientIps.Count == 0)
            return;

        var waiting = await dbContext
            .DelayLearningJobs
            .Where(x => !x.InWork && clientIps.Contains(x.ClientIp))
            .Select(x => x.ClientIp)
            .ToArrayAsync(cancellationToken);
        DelayLearningJob[] missing =
        [
            .. clientIps
                .Except(waiting, StringComparer.Ordinal)
                .Select(x => new DelayLearningJob
                {
                    ClientIp = x,
                    CreatedAt = now
                })
        ];
        if (missing.Length == 0)
            return;

        try
        {
            await dbContext.DelayLearningJobs.AddRangeAsync(missing, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation)
        {
            dbContext.ChangeTracker.Clear();
            foreach (var job in missing)
                await TryAddAsync(job, cancellationToken);
        }
        finally
        {
            dbContext.ChangeTracker.Clear();
        }
    }

    public async Task<DelayLearningJob?> TryClaimNextAsync(string claimedBy, DateTimeOffset now, DateTimeOffset staleBefore, CancellationToken cancellationToken)
    {
        var worker = claimedBy.Truncate(Entry.ClaimedByMaxLength);
        Expression<Func<DelayLearningJob, bool>> claimable = x =>
            x.InWork && x.ClaimedAt < staleBefore ||
            !x.InWork && !dbContext.DelayLearningJobs.Any(y => y.ClientIp == x.ClientIp && y.InWork);
        var candidates = await dbContext
            .DelayLearningJobs
            .Where(claimable)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .Take(ClaimCandidates)
            .ToArrayAsync(cancellationToken);

        foreach (var id in candidates)
        {
            int claimed;
            try
            {
                claimed = await dbContext
                    .DelayLearningJobs
                    .Where(x => x.Id == id)
                    .Where(claimable)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(x => x.InWork, true)
                            .SetProperty(x => x.ClaimedBy, worker)
                            .SetProperty(x => x.ClaimedAt, now),
                        cancellationToken);
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                continue;
            }

            if (claimed > 0)
                return await dbContext.DelayLearningJobs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.ClaimedBy == worker, cancellationToken);
        }

        return null;
    }

    public async Task<bool> CompleteAsync(long id, string claimedBy, CancellationToken cancellationToken)
    {
        var worker = claimedBy.Truncate(Entry.ClaimedByMaxLength);
        return await dbContext
            .DelayLearningJobs
            .Where(x => x.Id == id && x.ClaimedBy == worker)
            .ExecuteDeleteAsync(cancellationToken) > 0;
    }

    public Task<int> CountWaitingAsync(CancellationToken cancellationToken)
    {
        return dbContext.DelayLearningJobs.CountAsync(x => !x.InWork, cancellationToken);
    }

    public Task<int> ClearAsync(CancellationToken cancellationToken)
    {
        return dbContext.DelayLearningJobs.ExecuteDeleteAsync(cancellationToken);
    }

    private async Task TryAddAsync(DelayLearningJob job, CancellationToken cancellationToken)
    {
        await dbContext.DelayLearningJobs.AddAsync(job, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation)
        {
        }
        finally
        {
            dbContext.Entry(job).State = EntityState.Detached;
        }
    }
}