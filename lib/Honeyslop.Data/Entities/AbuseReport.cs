using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Honeyslop.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Entities;

[Table("abuse_report", Schema = "public")]
[PrimaryKey(nameof(Id))]
[Index(nameof(ClientIp))]
[Index(nameof(Status))]
public class AbuseReport
{
    public const int SubjectMaxLength = 256;
    public const int RequestedByMaxLength = 128;
    public const int SenderNameMaxLength = 128;
    public const int MessageIdMaxLength = 256;
    public const int SentFromMaxLength = 512;
    public const int ErrorMaxLength = 4096;

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [MaxLength(Entry.ClientIpMaxLength)]
    public string ClientIp { get; set; } = null!;

    public string[] Recipients { get; set; } = [];

    [MaxLength(SubjectMaxLength)]
    public string Subject { get; set; } = null!;

    public string Body { get; set; } = null!;

    [MaxLength(RequestedByMaxLength)]
    public string RequestedBy { get; set; } = null!;

    [MaxLength(SenderNameMaxLength)]
    public string? SenderName { get; set; }

    public AbuseReportStatus Status { get; set; }

    public int Attempts { get; set; }

    [MaxLength(Entry.ClaimedByMaxLength)]
    public string? ClaimedBy { get; set; }

    public DateTimeOffset? ClaimedAt { get; set; }

    public DateTimeOffset? RetryAt { get; set; }

    public int? AttachedRequests { get; set; }

    public long? LastRequestId { get; set; }

    [MaxLength(MessageIdMaxLength)]
    public string? MessageId { get; set; }

    [MaxLength(SentFromMaxLength)]
    public string? SentFrom { get; set; }

    [MaxLength(ErrorMaxLength)]
    public string? Error { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? SentAt { get; set; }
}
