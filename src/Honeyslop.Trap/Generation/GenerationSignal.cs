using Honeyslop.Trap.Options;
using Microsoft.Extensions.Options;

namespace Honeyslop.Trap.Generation;

internal sealed class GenerationSignal(IOptions<TrapOptions> options) : IGenerationSignal
{
    private readonly SemaphoreSlim _semaphore = new(0, Math.Max(1, options.Value.Workers));

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
