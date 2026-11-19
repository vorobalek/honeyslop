using System.Security.Claims;
using Honeyslop.Admin.Access;
using Honeyslop.Admin.Core.Exceptions;
using Honeyslop.Admin.Live.Contracts;
using Honeyslop.Admin.Views;

namespace Honeyslop.Admin.Live.Internals;

internal sealed partial class LiveView(
    string group,
    string key,
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

    public string Group => group;

    public LiveInterest Interest => interest;

    public HashSet<string> Members { get; } = [];

    public void Dispose()
    {
        _stop.Cancel();
    }

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
                ? sender.SendAsync(group, Missing, [key], cancellationToken)
                : sender.SendAsync(group, Update, [key, view], cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (AccessDeniedException exception)
        {
            await TrySendAsync(Denied, [key, exception.Message], cancellationToken);
        }
        catch (Exception exception)
        {
            LogFailed(logger, exception, key);
            await TrySendAsync(Failed, [key], cancellationToken);
        }
    }

    private async Task TrySendAsync(string method, object?[] arguments, CancellationToken cancellationToken)
    {
        try
        {
            await sender.SendAsync(group, method, arguments, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogFailed(logger, exception, key);
        }
    }

    [LoggerMessage(LogLevel.Warning, "Live view {Key} failed to refresh")]
    static partial void LogFailed(ILogger logger, Exception exception, string key);
}