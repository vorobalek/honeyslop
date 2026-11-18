using Honeyslop.Admin.Core.Enums;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record ReportsQuery(Sorting<ReportColumn>? Sort = null, int Page = 0);
