namespace Honeyslop.Trap.Generation;

public interface IGenerationSignal
{
    void Notify();
    Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);
}