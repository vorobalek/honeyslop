using Honeyslop.Admin.Core.Enums;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record ReportRequestDto(ReportOutcome Outcome, long? ReportId = null);
