using Honeyslop.Data.Contracts;

namespace Honeyslop.Data.Providers;

public interface ISettingsProvider
{
    ValueTask<HoneyslopSettings> GetAsync(CancellationToken cancellationToken);
    void Reset();
}