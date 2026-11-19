using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Entities;

[Table("resource_lock", Schema = "public")]
[PrimaryKey(nameof(Name))]
public class ResourceLock
{
    public const string DataProtectionKeys = "data_protection_keys";
    public const string Settings = "settings";

    [MaxLength(64)]
    public string Name { get; set; } = null!;

    public long Attempts { get; set; }
}
