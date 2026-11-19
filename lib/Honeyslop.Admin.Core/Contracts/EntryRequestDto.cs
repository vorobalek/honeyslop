namespace Honeyslop.Admin.Core.Contracts;

public sealed record EntryRequestDto(string Method,
    string Host,
    string Target,
    string? Headers,
    string? Body);