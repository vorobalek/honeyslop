using Honeyslop.Common;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class ClientNoiseRepository(ApplicationDbContext dbContext) : IClientNoiseRepository
{
    public Task<ClientNoise[]> GetAllAsync(CancellationToken cancellationToken)
    {
        return dbContext
            .ClientNoises
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
    }

    public Task<ClientNoise?> GetOrDefaultAsync(string clientIp, CancellationToken cancellationToken)
    {
        return dbContext
            .ClientNoises
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ClientIp == clientIp, cancellationToken);
    }

    public async Task SetAsync(string clientIp, NoiseTier? tier, string? changedBy, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var changer = changedBy?.Truncate(Entry.ChangedByMaxLength);
        if (tier is not { } chosen)
        {
            await dbContext.ClientNoises.Where(x => x.ClientIp == clientIp).ExecuteDeleteAsync(cancellationToken);
            return;
        }

        if (await UpdateAsync(clientIp, chosen, changer, now, cancellationToken) > 0)
            return;
        var added = new ClientNoise
        {
            ClientIp = clientIp,
            Tier = chosen,
            ChangedBy = changer,
            UpdatedAt = now
        };
        await dbContext.ClientNoises.AddAsync(added, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation)
        {
            dbContext.Entry(added).State = EntityState.Detached;
            await UpdateAsync(clientIp, chosen, changer, now, cancellationToken);
        }
    }

    private Task<int> UpdateAsync(string clientIp, NoiseTier tier, string? changedBy, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return dbContext
            .ClientNoises
            .Where(x => x.ClientIp == clientIp)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Tier, tier)
                    .SetProperty(x => x.ChangedBy, changedBy)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
    }
}