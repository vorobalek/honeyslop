using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Entities;

[Table("client_probe", Schema = "public")]
[PrimaryKey(nameof(ClientIp))]
public class ClientProbe
{
    [MaxLength(Entry.ClientIpMaxLength)]
    public string ClientIp { get; set; } = null!;

    public int DelayMilliseconds { get; set; }

    public int? TimeoutMilliseconds { get; set; }

    public int ProbeEvery { get; set; }

    public int AnswersSinceProbe { get; set; }

    public bool Probing { get; set; }

    public int? EarlyDropMilliseconds { get; set; }

    public int Version { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}