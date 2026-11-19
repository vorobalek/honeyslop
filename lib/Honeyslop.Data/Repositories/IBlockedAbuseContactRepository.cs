using Honeyslop.Data.Entities;

namespace Honeyslop.Data.Repositories;

public interface IBlockedAbuseContactRepository
{
    Task<BlockedAbuseContact[]> GetAnyOfAsync(IReadOnlyCollection<string> contacts, CancellationToken cancellationToken);
    Task<BlockedAbuseContact[]> GetPageAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<int> CountAsync(string? search, CancellationToken cancellationToken);
    Task<int> AddAsync(IReadOnlyCollection<BlockedAbuseContact> contacts, CancellationToken cancellationToken);
    Task<int> RemoveAsync(IReadOnlyCollection<string> contacts, CancellationToken cancellationToken);
}