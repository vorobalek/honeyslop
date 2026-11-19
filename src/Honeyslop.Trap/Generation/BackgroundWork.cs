namespace Honeyslop.Trap.Generation;

internal sealed class BackgroundWork : IAsyncDisposable
{
    private readonly CancellationTokenSource _stop;
    private readonly Task[] _tasks;

    private BackgroundWork(CancellationTokenSource stop, Task[] tasks)
    {
        _stop = stop;
        _tasks = tasks;
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        await Task.WhenAll(_tasks);
        _stop.Dispose();
    }

    public static BackgroundWork Start(CancellationToken cancellationToken, params Func<CancellationToken, Task>[] work)
    {
        var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        return new BackgroundWork(stop, [.. work.Select(x => x(stop.Token))]);
    }
}