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
    public required DbSet<BlockedAbuseContact> BlockedAbuseContacts { get; set; }
    public required DbSet<AbuseReportBounce> AbuseReportBounces { get; set; }
    public required DbSet<MailboxCursor> MailboxCursors { get; set; }
    public required DbSet<ClientDelay> ClientDelays { get; set; }
    public required DbSet<ClientProbe> ClientProbes { get; set; }
    public required DbSet<DelayJob> DelayJobs { get; set; }
}