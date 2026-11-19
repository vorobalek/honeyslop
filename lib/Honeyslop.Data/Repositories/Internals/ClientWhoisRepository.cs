using Honeyslop.Data.Entities;
using Honeyslop.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class ClientWhoisRepository(ApplicationDbContext dbContext) : IClientWhoisRepository
{
    public Task<ClientWhois?> GetOrDefaultAsync(string clientIp, CancellationToken cancellationToken)
    {
        return dbContext
            .ClientWhois
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ClientIp == clientIp, cancellationToken);
    }

    public async Task SaveAsync(string clientIp, string info, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (await UpdateAsync(clientIp, info, now, cancellationToken) > 0)
            return;

        var whois = new ClientWhois
        {
            ClientIp = clientIp,
            Info = info,
            UpdatedAt = now
        };
        await dbContext.ClientWhois.AddAsync(whois, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation)
        {
            dbContext.Entry(whois).State = EntityState.Detached;
            await UpdateAsync(clientIp, info, now, cancellationToken);
        }
    }

    private Task<int> UpdateAsync(string clientIp, string info, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return dbContext
            .ClientWhois
            .Where(x => x.ClientIp == clientIp)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Info, info)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
    }
}