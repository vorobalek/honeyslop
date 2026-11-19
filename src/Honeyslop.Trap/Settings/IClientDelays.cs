namespace Honeyslop.Trap.Settings;

public interface IClientDelays
{
    ValueTask<TimeSpan?> GetAsync(string clientIp, CancellationToken cancellationToken);
    void Reset();
}