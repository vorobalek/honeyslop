namespace Honeyslop.Admin.Core.Contracts;

public sealed record MaintenanceResultDto(int Expired,
    int Unused,
    int Pruned,
    int Deleted);