using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Honeyslop.Data.Entities.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Entities;

[Table("user_setting", Schema = "public")]
[PrimaryKey(nameof(UserId), nameof(Key))]
public class UserSetting : ISettingRow
{
    [MaxLength(256)]
    public string UserId { get; set; } = null!;

    [MaxLength(64)]
    public string Key { get; set; } = null!;

    [MaxLength(1024)]
    public string Value { get; set; } = null!;

    public DateTimeOffset UpdatedAt { get; set; }
}
