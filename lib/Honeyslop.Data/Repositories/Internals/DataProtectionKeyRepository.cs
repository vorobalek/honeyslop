using Honeyslop.Data.Entities;
using Honeyslop.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class DataProtectionKeyRepository(ApplicationDbContext dbContext) : IDataProtectionKeyRepository
{
    public IReadOnlyList<DataProtectionKey> GetAll()
    {
        return
        [
            .. dbContext
                .DataProtectionKeys
                .AsNoTracking()
                .OrderBy(x => x.Id)
        ];
    }

    public bool TryAdd(DataProtectionKey key)
    {
        using var transaction = dbContext.Database.BeginTransaction();
        dbContext.Lock(ResourceLock.DataProtectionKeys);
        if (key.ActivationDate is { } activation && dbContext.DataProtectionKeys.Any(x => x.ExpirationDate > activation))
            return false;
        dbContext.DataProtectionKeys.Add(key);
        dbContext.SaveChanges();
        transaction.Commit();
        return true;
    }
}