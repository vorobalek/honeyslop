using Honeyslop.Admin.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Honeyslop.Admin.Live.Internals;

internal sealed class HubLiveSender(IHubContext<AdminHub> hubContext) : ILiveSender
{
    public Task JoinAsync(string connectionId, string group, CancellationToken cancellationToken)
    {
        return hubContext.Groups.AddToGroupAsync(connectionId, group, cancellationToken);
    }

    public Task LeaveAsync(string connectionId, string group, CancellationToken cancellationToken)
    {
        return hubContext.Groups.RemoveFromGroupAsync(connectionId, group, cancellationToken);
    }

    public Task SendAsync(string group, string method, object?[] arguments, CancellationToken cancellationToken)
    {
        return hubContext.Clients.Group(group).SendCoreAsync(method, arguments, cancellationToken);
    }
}