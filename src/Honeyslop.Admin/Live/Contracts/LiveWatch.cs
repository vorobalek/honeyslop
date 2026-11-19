using System.Security.Claims;
using Honeyslop.Admin.Live.Contracts.Watches;
using Honeyslop.Admin.Views;

namespace Honeyslop.Admin.Live.Contracts;

public sealed record LiveWatch(
    string ConnectionId,
    string Key,
    string Audience,
    WatchRequest Request,
    ClaimsPrincipal User,
    LiveInterest Interest,
    Func<IViewBuilder, CancellationToken, Task<object?>> Build);
