using Honeyslop.Data.Repositories;
using Microsoft.Extensions.Options;

namespace Honeyslop.Admin.Clients;

internal sealed class UserAgentProvider(ISettingsRepository settingsRepository, IOptions<UserAgentOptions> options) : IUserAgentProvider
{
    private readonly Lock _lock = new();
    private Task<string>? _userAgent;

    public Task<string> GetAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
            return _userAgent ??= LoadAsync(cancellationToken);
    }

    private async Task<string> LoadAsync(CancellationToken cancellationToken)
    {
        return (await settingsRepository.GetAsync(cancellationToken)).UserAgent ?? options.Value.Default;
    }
}
