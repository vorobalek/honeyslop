using Honeyslop.Admin.Core.Contracts;

namespace Honeyslop.Admin.Core.Services;

public interface IBlockedContactsService
{
    const int PageSize = 50;
    const int MaxPerChange = 1000;

    Task<BlockedContactsPageDto> GetPageAsync(BlockedContactsQuery query, CancellationToken cancellationToken);
    Task<ContactsChangeDto> BlockAsync(IReadOnlyCollection<string> contacts, string? note, CancellationToken cancellationToken);
    Task<ContactsChangeDto> UnblockAsync(IReadOnlyCollection<string> contacts, CancellationToken cancellationToken);
}