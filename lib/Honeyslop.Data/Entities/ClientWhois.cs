using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Entities;

[Table("client_whois", Schema = "public")]
[PrimaryKey(nameof(ClientIp))]
public class ClientWhois
{
    [MaxLength(Entry.ClientIpMaxLength)]
    public string ClientIp { get; set; } = null!;

    [Column(TypeName = "jsonb")]
    public string Info { get; set; } = null!;

    public DateTimeOffset UpdatedAt { get; set; }
}