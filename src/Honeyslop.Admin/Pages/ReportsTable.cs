using Honeyslop.Data.Entities;

namespace Honeyslop.Admin.Pages;

public sealed record ReportsTable(AbuseReport[] Reports, bool ShowClient, bool ShowRequester);
