using Honeyslop.Data.Repositories;

namespace Honeyslop.Admin.Services;

public interface ICacheMaintenance
{
    Task<MaintenanceResult> RunAsync(HoneyslopSettings settings, CancellationToken cancellationToken);
}
