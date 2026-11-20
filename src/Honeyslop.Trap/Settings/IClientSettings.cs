namespace Honeyslop.Trap.Settings;

public interface IClientSettings
{
    ValueTask<PersonalSettings> GetAsync(string clientIp, CancellationToken cancellationToken);
    void Reset();
}