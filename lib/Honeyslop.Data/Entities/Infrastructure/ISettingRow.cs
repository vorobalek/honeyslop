namespace Honeyslop.Data.Entities.Infrastructure;

public interface ISettingRow
{
    string Key { get; }
    string Value { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
}