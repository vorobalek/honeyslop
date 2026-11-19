using Honeyslop.Data.Enums;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record ReportDto(
    long Id,
    string ClientIp,
    string[] Recipients,
    string Subject,
    AbuseReportState State,
    string? RequestedBy,
    int Attempts,
    int? AttachedRequests,
    string? Error,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt,
    DateTimeOffset? RetryAt);