namespace Honeyslop.Admin.Services;

public sealed record MaintenanceResult(int Expired, int Removed, int Pruned);
