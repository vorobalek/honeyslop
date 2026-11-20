using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Honeyslop.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Entities;

[Table("client_noise", Schema = "public")]
[PrimaryKey(nameof(ClientIp))]
public class ClientNoise
{
    [MaxLength(Entry.ClientIpMaxLength)]
    public string ClientIp { get; set; } = null!;

    public NoiseTier Tier { get; set; }

    [MaxLength(Entry.ChangedByMaxLength)]
    public string? ChangedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}