using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Entities;

[Table("mailbox_cursor", Schema = "public")]
[PrimaryKey(nameof(Folder))]
public class MailboxCursor
{
    public const int FolderMaxLength = 256;

    [MaxLength(FolderMaxLength)]
    public string Folder { get; set; } = null!;

    public long UidValidity { get; set; }

    public long LastUid { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}