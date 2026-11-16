using Honeyslop.Data.Repositories;

namespace Honeyslop.Trap.Settings;

public interface ISettingsProvider
{
    ValueTask<HoneyslopSettings> GetAsync(CancellationToken cancellationToken);
}
