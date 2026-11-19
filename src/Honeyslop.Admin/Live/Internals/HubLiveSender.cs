using Honeyslop.Admin.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Honeyslop.Admin.Live.Internals;

internal sealed class HubLiveSender(IHubContext<AdminHub> hubContext) : ILiveSender
{
    public Task SendAsync(string connectionId, string method, object?[] arguments, CancellationToken cancellationToken)
    {
        return hubContext.Clients.Client(connectionId).SendCoreAsync(method, arguments, cancellationToken);
    }
}
