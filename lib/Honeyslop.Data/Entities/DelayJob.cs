using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Entities;

[Table("delay_job", Schema = "public")]
[PrimaryKey(nameof(Id))]
[Index(nameof(ClaimedAt), nameof(Id))]
public class DelayJob
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [MaxLength(Entry.ClientIpMaxLength)]
    public string ClientIp { get; set; } = null!;

    public bool Answered { get; set; }

    public int DurationMs { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    [MaxLength(Entry.ClaimedByMaxLength)]
    public string? ClaimedBy { get; set; }

    public DateTimeOffset? ClaimedAt { get; set; }
}