using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Honeyslop.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Entities;

[Table("entry", Schema = "public")]
[PrimaryKey(nameof(Id))]
[Index(nameof(Status))]
[Index(nameof(Host))]
[Index(nameof(CreatedAt))]
[Index(nameof(LastHitAt))]
public class Entry
{
    public const int MethodMaxLength = 16;
    public const int HostMaxLength = 255;
    public const int TargetMaxLength = 2048;
    public const int ErrorMaxLength = 4096;
    public const int ClientIpMaxLength = 64;
    public const int UserAgentMaxLength = 1024;
    public const int ClaimedByMaxLength = 128;
    public const int ModelMaxLength = 128;
    public const int GeneratorMaxLength = 128;
    public const int RequestHeadersMaxLength = 8192;
    public const int RequestBodyMaxLength = 65536;
    public const int ResponseBodyMaxLength = 65536;
    public const int ResponseHeadersMaxCount = 30;
    public const int GuidanceMaxLength = 2048;
    public const int ResponseDelayMaxMilliseconds = 600_000;
    public const int ChangedByMaxLength = 128;

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public byte[] KeyHash { get; set; } = null!;

    [MaxLength(MethodMaxLength)]
    public string Method { get; set; } = null!;

    [MaxLength(HostMaxLength)]
    public string Host { get; set; } = null!;

    [MaxLength(TargetMaxLength)]
    public string Target { get; set; } = null!;

    public EntryStatus Status { get; set; }

    public int Attempts { get; set; }

    [MaxLength(ClaimedByMaxLength)]
    public string? ClaimedBy { get; set; }

    public DateTimeOffset? ClaimedAt { get; set; }

    [MaxLength(ErrorMaxLength)]
    public string? Error { get; set; }

    [MaxLength(ClientIpMaxLength)]
    public string? ClientIp { get; set; }

    [MaxLength(UserAgentMaxLength)]
    public string? UserAgent { get; set; }

    [MaxLength(RequestHeadersMaxLength)]
    public string? RequestHeaders { get; set; }

    [MaxLength(RequestBodyMaxLength)]
    public string? RequestBody { get; set; }

    public int? ResponseStatus { get; set; }

    public List<ResponseHeader> ResponseHeaders { get; set; } = [];

    [MaxLength(ResponseBodyMaxLength)]
    public string? ResponseBody { get; set; }

    [MaxLength(ModelMaxLength)]
    public string? Model { get; set; }

    [MaxLength(GeneratorMaxLength)]
    public string? Generator { get; set; }

    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }

    public int? GenerationMs { get; set; }

    public long Hits { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? GeneratedAt { get; set; }

    public DateTimeOffset? LastHitAt { get; set; }

    public DateTimeOffset? RequestedAt { get; set; }

    [MaxLength(GuidanceMaxLength)]
    public string? Guidance { get; set; }

    [MaxLength(ChangedByMaxLength)]
    public string? ChangedBy { get; set; }

    public int? ResponseDelayMilliseconds { get; set; }

    public NoiseTier? Noise { get; set; }
}