namespace Honeyslop.Admin.Services;

public sealed record MaintenanceResult(int Expired, int Unused, int Pruned, int Deleted);
