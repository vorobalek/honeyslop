namespace Honeyslop.Worker.Generation;

public interface IGenerationSignal
{
    void Notify();
    Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);
}