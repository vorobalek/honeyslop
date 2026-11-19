using Honeyslop.Data.Entities;
using Honeyslop.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class ClientProbeRepository(ApplicationDbContext dbContext) : IClientProbeRepository
{
    public Task<ClientProbe?> GetOrDefaultAsync(string clientIp, CancellationToken cancellationToken)
    {
        return dbContext
            .ClientProbes
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ClientIp == clientIp, cancellationToken);
    }

    public Task<ClientProbe[]> GetAllAsync(CancellationToken cancellationToken)
    {
        return dbContext
            .ClientProbes
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
    }

    public Task<ClientProbe[]> GetAnyOfAsync(IReadOnlyCollection<string> clientIps, CancellationToken cancellationToken)
    {
        return dbContext
            .ClientProbes
            .AsNoTracking()
            .Where(x => clientIps.Contains(x.ClientIp))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> TryAddAsync(ClientProbe probe, CancellationToken cancellationToken)
    {
        await dbContext.ClientProbes.AddAsync(probe, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation)
        {
            return false;
        }
        finally
        {
            dbContext.Entry(probe).State = EntityState.Detached;
        }
    }

    public async Task<bool> TryReplaceAsync(ClientProbe probe, int expectedVersion, CancellationToken cancellationToken)
    {
        return await dbContext
            .ClientProbes
            .Where(x => x.ClientIp == probe.ClientIp && x.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.DelayMilliseconds, probe.DelayMilliseconds)
                    .SetProperty(x => x.TimeoutMilliseconds, probe.TimeoutMilliseconds)
                    .SetProperty(x => x.ProbeEvery, probe.ProbeEvery)
                    .SetProperty(x => x.AnswersSinceProbe, probe.AnswersSinceProbe)
                    .SetProperty(x => x.Probing, probe.Probing)
                    .SetProperty(x => x.EarlyDropMilliseconds, probe.EarlyDropMilliseconds)
                    .SetProperty(x => x.Version, expectedVersion + 1)
                    .SetProperty(x => x.UpdatedAt, probe.UpdatedAt),
                cancellationToken) > 0;
    }

    public Task<int> ClearAsync(CancellationToken cancellationToken)
    {
        return dbContext.ClientProbes.ExecuteDeleteAsync(cancellationToken);
    }
}