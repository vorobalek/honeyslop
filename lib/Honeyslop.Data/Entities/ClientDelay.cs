using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Entities;

[Table("client_delay", Schema = "public")]
[PrimaryKey(nameof(ClientIp))]
public class ClientDelay
{
    [MaxLength(Entry.ClientIpMaxLength)]
    public string ClientIp { get; set; } = null!;

    public int ResponseDelayMilliseconds { get; set; }

    [MaxLength(Entry.ChangedByMaxLength)]
    public string? ChangedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}