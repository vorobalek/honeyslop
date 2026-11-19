using Honeyslop.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Honeyslop.Data.Extensions;

internal static class ResourceLockExtensions
{
    extension(ApplicationDbContext dbContext)
    {
        public void Lock(string name)
        {
            EnsureLocked(name, dbContext.ResourceLocks.Where(x => x.Name == name).ExecuteUpdate(Attempt));
        }

        public async Task LockAsync(string name, CancellationToken cancellationToken)
        {
            EnsureLocked(name, await dbContext.ResourceLocks.Where(x => x.Name == name).ExecuteUpdateAsync(Attempt, cancellationToken));
        }
    }

    private static void Attempt(UpdateSettersBuilder<ResourceLock> setters)
    {
        setters.SetProperty(x => x.Attempts, x => x.Attempts + 1);
    }

    private static void EnsureLocked(string name, int locked)
    {
        if (locked == 0)
            throw new InvalidOperationException($"The resource lock '{name}' is missing, the database is not migrated.");
    }
}
