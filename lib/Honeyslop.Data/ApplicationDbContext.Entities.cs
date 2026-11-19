using Honeyslop.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data;

public partial class ApplicationDbContext
{
    public required DbSet<Entry> Entries { get; set; }
    public required DbSet<Setting> Settings { get; set; }
    public required DbSet<RequestRecord> Requests { get; set; }
    public required DbSet<ClientWhois> ClientWhois { get; set; }
    public required DbSet<AbuseReport> AbuseReports { get; set; }
    public required DbSet<UserSetting> UserSettings { get; set; }
    public required DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    public required DbSet<ResourceLock> ResourceLocks { get; set; }
}
