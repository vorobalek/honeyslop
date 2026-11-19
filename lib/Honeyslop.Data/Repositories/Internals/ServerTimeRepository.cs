using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class ServerTimeRepository(ApplicationDbContext dbContext) : IServerTimeRepository
{
    public Task<DateTimeOffset> GetNowAsync(CancellationToken cancellationToken)
    {
        return dbContext
            .Database
            .SqlQuery<DateTimeOffset>($"select now() as \"Value\"")
            .SingleAsync(cancellationToken);
    }
}