using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Entities;

[Table("data_protection_key", Schema = "public")]
[PrimaryKey(nameof(Id))]
public class DataProtectionKey
{
    public const int FriendlyNameMaxLength = 256;

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [MaxLength(FriendlyNameMaxLength)]
    public string? FriendlyName { get; set; }

    public string Xml { get; set; } = null!;

    public DateTimeOffset? ActivationDate { get; set; }

    public DateTimeOffset? ExpirationDate { get; set; }
}
