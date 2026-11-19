namespace Honeyslop.Admin.Core.Contracts;

public sealed record BouncedContactsPageDto(PagedDto<BouncedContactDto> Contacts,
    DateTimeOffset AsOf);