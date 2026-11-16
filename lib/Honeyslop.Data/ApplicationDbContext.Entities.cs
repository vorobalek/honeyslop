using Honeyslop.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data;

public partial class ApplicationDbContext
{
    public required DbSet<Entry> Entries { get; set; }
    public required DbSet<Setting> Settings { get; set; }
    public required DbSet<RequestRecord> Requests { get; set; }
}
