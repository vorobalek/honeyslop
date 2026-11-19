namespace Honeyslop.Trap.Settings;

public interface IClientDelays
{
    ValueTask<PersonalDelay> GetAsync(string clientIp, CancellationToken cancellationToken);
    void Reset();
}