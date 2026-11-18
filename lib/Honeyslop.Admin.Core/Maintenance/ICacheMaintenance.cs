using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Data.Contracts;

namespace Honeyslop.Admin.Core.Maintenance;

public interface ICacheMaintenance
{
    Task<MaintenanceResultDto> RunAsync(HoneyslopSettings settings, CancellationToken cancellationToken);
}
