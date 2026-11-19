using Microsoft.Extensions.Options;

namespace Honeyslop.Worker.Generation.Internals;

internal sealed class GenerationSignal(IOptions<WorkerOptions> options) : IGenerationSignal
{
    private readonly SemaphoreSlim _semaphore = new(0, Math.Max(1, options.Value.GenerationWorkers));

    public void Notify()
    {
        try
        {
            _semaphore.Release();
        }
        catch (SemaphoreFullException)
        {
        }
    }

    public Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return _semaphore.WaitAsync(timeout, cancellationToken);
    }
}