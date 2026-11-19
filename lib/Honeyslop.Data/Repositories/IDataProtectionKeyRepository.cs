using Honeyslop.Data.Entities;

namespace Honeyslop.Data.Repositories;

public interface IDataProtectionKeyRepository
{
    IReadOnlyList<DataProtectionKey> GetAll();
    bool TryAdd(DataProtectionKey key);
}
