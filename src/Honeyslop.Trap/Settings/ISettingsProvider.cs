using Honeyslop.Data.Contracts;

namespace Honeyslop.Trap.Settings;

public interface ISettingsProvider
{
    ValueTask<HoneyslopSettings> GetAsync(CancellationToken cancellationToken);
}
