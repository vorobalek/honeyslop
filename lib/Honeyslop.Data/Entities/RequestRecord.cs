using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Honeyslop.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Entities;

[Table("request", Schema = "public")]
[PrimaryKey(nameof(Id))]
[Index(nameof(ReceivedAt))]
[Index(nameof(ClientIp), nameof(ReceivedAt))]
[Index(nameof(EntryId))]
public class RequestRecord
{
    public const int HeadersMaxLength = 8192;
    public const int BodyMaxLength = 65536;

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    [MaxLength(Entry.ClientIpMaxLength)]
    public string ClientIp { get; set; } = null!;

    [MaxLength(Entry.MethodMaxLength)]
    public string Method { get; set; } = null!;

    [MaxLength(Entry.HostMaxLength)]
    public string Host { get; set; } = null!;

    [MaxLength(Entry.TargetMaxLength)]
    public string Target { get; set; } = null!;

    [MaxLength(Entry.UserAgentMaxLength)]
    public string? UserAgent { get; set; }

    [MaxLength(HeadersMaxLength)]
    public string? Headers { get; set; }

    [MaxLength(BodyMaxLength)]
    public string? Body { get; set; }

    public RequestOutcome Outcome { get; set; }

    public int ResponseStatus { get; set; }

    public long? EntryId { get; set; }

    public int DurationMs { get; set; }

    [MaxLength(Entry.ClaimedByMaxLength)]
    public string? Instance { get; set; }
}
