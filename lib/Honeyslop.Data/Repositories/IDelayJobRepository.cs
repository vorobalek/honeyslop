using Honeyslop.Data.Entities;

namespace Honeyslop.Data.Repositories;

public interface IDelayJobRepository
{
    Task AddRangeAsync(IReadOnlyCollection<DelayJob> jobs, CancellationToken cancellationToken);
    Task<DelayJob?> TryClaimNextAsync(string claimedBy, DateTimeOffset now, DateTimeOffset staleBefore, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(long id, string claimedBy, CancellationToken cancellationToken);
    Task<int> ClearAsync(CancellationToken cancellationToken);
}