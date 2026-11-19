using Honeyslop.Admin.Live.Contracts;
using Honeyslop.Admin.Live.Contracts.Watches;
using Honeyslop.Admin.Live.Enums;
using Honeyslop.Admin.Views;

namespace Honeyslop.Admin.Live;

public interface IWatchHandler
{
    Type RequestType { get; }
    WatchAudience Audience { get; }
    LiveInterest InterestIn(WatchRequest request);
    Task<object?> BuildAsync(IViewBuilder views, WatchRequest request, CancellationToken cancellationToken);
}
