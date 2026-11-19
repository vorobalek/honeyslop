using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Entities;

[Table("delay_learning_job", Schema = "public")]
[PrimaryKey(nameof(Id))]
[Index(nameof(ClientIp), nameof(InWork), IsUnique = true)]
public class DelayLearningJob
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [MaxLength(Entry.ClientIpMaxLength)]
    public string ClientIp { get; set; } = null!;

    public bool InWork { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    [MaxLength(Entry.ClaimedByMaxLength)]
    public string? ClaimedBy { get; set; }

    public DateTimeOffset? ClaimedAt { get; set; }
}