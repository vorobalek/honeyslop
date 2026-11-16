using Honeyslop.Trap.Responses;

namespace Honeyslop.Trap.Generation;

public interface IEntryWatcher
{
    Task<CachedResponse?> WaitAsync(long entryId, TimeSpan timeout, CancellationToken cancellationToken);
    void Complete(long entryId, CachedResponse? response);
}
