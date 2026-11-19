using Honeyslop.Data.Entities;

namespace Honeyslop.Data.Repositories;

public interface IDelayLearningJobRepository
{
    Task EnqueueAsync(IReadOnlyCollection<string> clientIps, DateTimeOffset now, CancellationToken cancellationToken);
    Task<DelayLearningJob?> TryClaimNextAsync(string claimedBy, DateTimeOffset now, DateTimeOffset staleBefore, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(long id, string claimedBy, CancellationToken cancellationToken);
    Task<int> CountWaitingAsync(CancellationToken cancellationToken);
    Task<int> ClearAsync(CancellationToken cancellationToken);
}