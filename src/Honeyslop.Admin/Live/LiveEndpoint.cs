using System.Net.WebSockets;
using System.Text;
using Honeyslop.Admin.Access;

namespace Honeyslop.Admin.Live;

internal static class LiveEndpoint
{
    public static async Task HandleAsync(HttpContext httpContext)
    {
        if (!httpContext.WebSockets.IsWebSocketRequest)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var hub = httpContext.RequestServices.GetRequiredService<LiveUpdateHub>();
        var lifetime = httpContext.RequestServices.GetRequiredService<IHostApplicationLifetime>();
        var publicView = httpContext.User.CanOperate ? null : httpContext.RequestServices.GetRequiredService<IPublicView>();
        using var socket = await httpContext.WebSockets.AcceptWebSocketAsync();
        using var subscription = hub.Subscribe();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(httpContext.RequestAborted, lifetime.ApplicationStopping);

        var receiving = ReceiveUntilClosedAsync(socket, stop.Token);
        var sending = SendAsync(socket, subscription, publicView, stop.Token);
        await Task.WhenAny(receiving, sending);
        await stop.CancelAsync();
        try
        {
            await Task.WhenAll(receiving, sending);
        }
        catch (Exception exception) when (exception is OperationCanceledException or WebSocketException)
        {
        }

        if (socket.State == WebSocketState.Open)
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
    }

    private static async Task SendAsync(
        WebSocket socket,
        LiveUpdateHub.Subscription subscription,
        IPublicView? publicView,
        CancellationToken cancellationToken)
    {
        await foreach (var message in subscription.Reader.ReadAllAsync(cancellationToken))
        {
            if (publicView is not null && !await publicView.IsEnabledAsync(cancellationToken))
                return;
            await socket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, cancellationToken);
        }
    }

    private static async Task ReceiveUntilClosedAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[256];
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
                return;
        }
    }
}
