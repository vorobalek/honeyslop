using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Honeyslop.Data.Entities.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Entities;

[Table("setting", Schema = "public")]
[PrimaryKey(nameof(Key))]
public class Setting : ISettingRow
{
    [MaxLength(64)]
    public string Key { get; set; } = null!;

    [MaxLength(1024)]
    public string Value { get; set; } = null!;

    public DateTimeOffset UpdatedAt { get; set; }
}
