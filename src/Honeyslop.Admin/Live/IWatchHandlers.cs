using Honeyslop.Admin.Live.Contracts.Watches;

namespace Honeyslop.Admin.Live;

public interface IWatchHandlers
{
    IWatchHandler For(WatchRequest request);
}
