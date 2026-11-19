using System.Security.Claims;
using Honeyslop.Admin.Access;
using Honeyslop.Admin.Core.Exceptions;
using Honeyslop.Admin.Live.Contracts;
using Honeyslop.Admin.Views;

namespace Honeyslop.Admin.Live.Internals;

internal sealed partial class LiveSubscription(
    string connectionId,
    int watchId,
    ClaimsPrincipal user,
    LiveInterest interest,
    Func<IViewBuilder, CancellationToken, Task<object?>> build,
    IServiceScopeFactory scopeFactory,
    ILiveSender sender,
    ILogger logger) : IDisposable
{
    public const string Update = "update";
    public const string Missing = "missing";
    public const string Denied = "denied";
    public const string Failed = "failed";

    private const int Idle = 0;
    private const int Running = 1;
    private const int RunningDirty = 2;

    private readonly CancellationTokenSource _stop = new();
    private int _state;

    public LiveInterest Interest => interest;

    public void Invalidate()
    {
        while (true)
        {
            var state = Volatile.Read(ref _state);
            if (state == RunningDirty)
                return;
            if (Interlocked.CompareExchange(ref _state, state == Idle ? Running : RunningDirty, state) != state)
                continue;
            if (state == Idle)
                _ = Task.Run(RunAsync);
            return;
        }
    }

    public void Dispose()
    {
        _stop.Cancel();
    }

    private async Task RunAsync()
    {
        while (true)
        {
            await RefreshAsync(_stop.Token);
            if (Interlocked.CompareExchange(ref _state, Idle, Running) == Running)
                return;
            Volatile.Write(ref _state, Running);
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IViewerScope>().Use(user);
            var view = await build(scope.ServiceProvider.GetRequiredService<IViewBuilder>(), cancellationToken);
            await (view is null
                ? sender.SendAsync(connectionId, Missing, [watchId], cancellationToken)
                : sender.SendAsync(connectionId, Update, [watchId, view], cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (AccessDeniedException exception)
        {
            await TrySendAsync(Denied, [watchId, exception.Message], cancellationToken);
        }
        catch (Exception exception)
        {
            LogFailed(logger, exception, watchId);
            await TrySendAsync(Failed, [watchId], cancellationToken);
        }
    }

    private async Task TrySendAsync(string method, object?[] arguments, CancellationToken cancellationToken)
    {
        try
        {
            await sender.SendAsync(connectionId, method, arguments, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogFailed(logger, exception, watchId);
        }
    }

    [LoggerMessage(LogLevel.Warning, "Live view {WatchId} failed to refresh")]
    static partial void LogFailed(ILogger logger, Exception exception, int watchId);
}
