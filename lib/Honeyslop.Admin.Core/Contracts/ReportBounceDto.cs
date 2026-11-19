using Honeyslop.Data.Enums;

namespace Honeyslop.Admin.Core.Contracts;

public sealed record ReportBounceDto(string Recipient,
    BounceAction Action,
    string? Status,
    string? Diagnostic,
    string? Mta,
    DateTimeOffset BouncedAt);