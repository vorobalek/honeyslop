using System.Security.Claims;
using Honeyslop.Admin.Live.Contracts;
using Honeyslop.Admin.Views;

namespace Honeyslop.Admin.Live;

public interface ILiveSubscriptions
{
    int Count { get; }

    void Watch(
        string connectionId,
        int watchId,
        ClaimsPrincipal user,
        LiveInterest interest,
        Func<IViewBuilder, CancellationToken, Task<object?>> build);

    void Unwatch(string connectionId, int watchId);
    void Drop(string connectionId);
    void Publish(LiveChange change);
    void PublishEverything();
}
