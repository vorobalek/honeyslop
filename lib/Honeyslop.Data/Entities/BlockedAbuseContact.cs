using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Entities;

[Table("blocked_abuse_contact", Schema = "public")]
[PrimaryKey(nameof(Contact))]
[Index(nameof(CreatedAt))]
public class BlockedAbuseContact
{
    public const int ContactMaxLength = 320;
    public const int NoteMaxLength = 256;

    [MaxLength(ContactMaxLength)]
    public string Contact { get; set; } = null!;

    [MaxLength(NoteMaxLength)]
    public string? Note { get; set; }

    [MaxLength(Entry.ClaimedByMaxLength)]
    public string? CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}