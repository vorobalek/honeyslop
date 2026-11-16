using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Entities;

[Table("setting", Schema = "public")]
[PrimaryKey(nameof(Key))]
public class Setting
{
    [MaxLength(64)]
    public string Key { get; set; } = null!;

    [MaxLength(1024)]
    public string Value { get; set; } = null!;

    public DateTimeOffset UpdatedAt { get; set; }
}
