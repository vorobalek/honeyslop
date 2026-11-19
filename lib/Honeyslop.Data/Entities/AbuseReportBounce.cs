using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Honeyslop.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Entities;

[Table("abuse_report_bounce", Schema = "public")]
[PrimaryKey(nameof(Id))]
[Index(nameof(ReportId))]
[Index(nameof(Recipient), nameof(BouncedAt))]
[Index(nameof(NotificationId), nameof(Recipient), IsUnique = true)]
public class AbuseReportBounce
{
    public const int NotificationIdMaxLength = 256;
    public const int StatusMaxLength = 32;
    public const int DiagnosticMaxLength = 1024;
    public const int MtaMaxLength = 256;

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long ReportId { get; set; }

    [MaxLength(BlockedAbuseContact.ContactMaxLength)]
    public string Recipient { get; set; } = null!;

    public BounceAction Action { get; set; }

    [MaxLength(StatusMaxLength)]
    public string? Status { get; set; }

    [MaxLength(DiagnosticMaxLength)]
    public string? Diagnostic { get; set; }

    [MaxLength(MtaMaxLength)]
    public string? Mta { get; set; }

    [MaxLength(NotificationIdMaxLength)]
    public string NotificationId { get; set; } = null!;

    public DateTimeOffset BouncedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}