using Honeyslop.Common;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class ClientDelayRepository(ApplicationDbContext dbContext) : IClientDelayRepository
{
    public Task<ClientDelay[]> GetAllAsync(CancellationToken cancellationToken)
    {
        return dbContext
            .ClientDelays
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
    }

    public Task<ClientDelay?> GetOrDefaultAsync(string clientIp, CancellationToken cancellationToken)
    {
        return dbContext
            .ClientDelays
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ClientIp == clientIp, cancellationToken);
    }

    public async Task SetAsync(string clientIp, int? milliseconds, string? changedBy, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var changer = changedBy?.Truncate(Entry.ChangedByMaxLength);
        if (milliseconds is not { } delay)
        {
            await dbContext.ClientDelays.Where(x => x.ClientIp == clientIp).ExecuteDeleteAsync(cancellationToken);
            return;
        }

        if (await UpdateAsync(clientIp, delay, changer, now, cancellationToken) > 0)
            return;
        var added = new ClientDelay
        {
            ClientIp = clientIp,
            ResponseDelayMilliseconds = delay,
            ChangedBy = changer,
            UpdatedAt = now
        };
        await dbContext.ClientDelays.AddAsync(added, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation)
        {
            dbContext.Entry(added).State = EntityState.Detached;
            await UpdateAsync(clientIp, delay, changer, now, cancellationToken);
        }
    }

    private Task<int> UpdateAsync(string clientIp, int milliseconds, string? changedBy, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return dbContext
            .ClientDelays
            .Where(x => x.ClientIp == clientIp)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ResponseDelayMilliseconds, milliseconds)
                    .SetProperty(x => x.ChangedBy, changedBy)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
    }
}