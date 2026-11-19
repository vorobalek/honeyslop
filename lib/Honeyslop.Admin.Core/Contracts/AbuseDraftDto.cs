namespace Honeyslop.Admin.Core.Contracts;

public sealed record AbuseDraftDto(LetterDto Letter,
    string? SenderName,
    string? FromAddress,
    int ArchivedRequests,
    string ArchiveFileName);