using Honeyslop.Admin.Live.Contracts;
using Honeyslop.Admin.Live.Contracts.Watches;
using Honeyslop.Admin.Live.Enums;
using Honeyslop.Admin.Views;

namespace Honeyslop.Admin.Live.Internals;

internal sealed class WatchHandler<TRequest, TView>(
    WatchAudience audience,
    Func<TRequest, LiveInterest> interest,
    Func<IViewBuilder, TRequest, CancellationToken, Task<TView>> build) : IWatchHandler
    where TRequest : WatchRequest
{
    public Type RequestType => typeof(TRequest);

    public WatchAudience Audience => audience;

    public LiveInterest InterestIn(WatchRequest request)
    {
        return interest(Typed(request));
    }

    public async Task<object?> BuildAsync(IViewBuilder views, WatchRequest request, CancellationToken cancellationToken)
    {
        return await build(views, Typed(request), cancellationToken);
    }

    private static TRequest Typed(WatchRequest request)
    {
        return request as TRequest ?? throw new ArgumentException($"Expected {typeof(TRequest).Name}, got {request.GetType().Name}.", nameof(request));
    }
}
