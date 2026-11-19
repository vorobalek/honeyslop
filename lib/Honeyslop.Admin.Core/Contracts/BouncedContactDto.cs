namespace Honeyslop.Admin.Core.Contracts;

public sealed record BouncedContactDto(string Contact,
    int Bounces,
    int Reports,
    DateTimeOffset LastBouncedAt,
    long LastReportId,
    string? LastStatus,
    string? LastDiagnostic,
    bool Blocked);