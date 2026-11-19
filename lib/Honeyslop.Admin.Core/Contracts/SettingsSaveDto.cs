namespace Honeyslop.Admin.Core.Contracts;

public sealed record SettingsSaveDto(IReadOnlyDictionary<string, string> Errors,
    MaintenanceResultDto? Maintenance)
{
    public bool Saved => Errors.Count == 0;
}