using Honeyslop.Admin.Access;
using Honeyslop.Admin.Core.Exceptions;
using Microsoft.AspNetCore.SignalR;

namespace Honeyslop.Admin.Hubs;

internal sealed class AdminHubFilter : IHubFilter
{
    public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        if (context.Context.GetHttpContext()?.Request is { } request &&
            request.Headers.Origin.Count > 0 &&
            !request.IsSameOrigin)
            throw new HubException("Connections are accepted from this site only.");
        return next(context);
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        if (invocationContext.Context.User is { } user)
            invocationContext.ServiceProvider.GetRequiredService<IViewerScope>().Use(user);
        try
        {
            return await next(invocationContext);
        }
        catch (AccessDeniedException exception)
        {
            throw new HubException(exception.Message);
        }
    }
}