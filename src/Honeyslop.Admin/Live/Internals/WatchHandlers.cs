using Honeyslop.Admin.Live.Contracts.Watches;

namespace Honeyslop.Admin.Live.Internals;

internal sealed class WatchHandlers(IEnumerable<IWatchHandler> handlers) : IWatchHandlers
{
    private readonly Dictionary<Type, IWatchHandler> _byType = handlers.ToDictionary(x => x.RequestType);

    public IWatchHandler For(WatchRequest request)
    {
        return _byType.TryGetValue(request.GetType(), out var handler)
            ? handler
            : throw new InvalidOperationException($"No view is registered for {request.GetType().Name}.");
    }
}