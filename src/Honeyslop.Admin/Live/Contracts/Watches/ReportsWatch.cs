using Honeyslop.Admin.Core.Contracts;

namespace Honeyslop.Admin.Live.Contracts.Watches;

public sealed record ReportsWatch(ReportsQuery Query) : WatchRequest;