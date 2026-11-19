using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Data.Contracts;

namespace Honeyslop.Admin.Core.Services;

public interface ISettingsService
{
    Task<SettingsDto> GetAsync(CancellationToken cancellationToken);
    Task<SettingsSaveDto> SaveAsync(HoneyslopSettings settings, CancellationToken cancellationToken);
    Task<string?> SavePersonalAsync(string? senderName, CancellationToken cancellationToken);
}