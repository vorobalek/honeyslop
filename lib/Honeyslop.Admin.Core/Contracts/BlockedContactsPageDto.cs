namespace Honeyslop.Admin.Core.Contracts;

public sealed record BlockedContactsPageDto(PagedDto<BlockedContactDto> Contacts,
    DateTimeOffset AsOf);