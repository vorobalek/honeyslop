using Honeyslop.Admin.Core.Access;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Repositories;
using Honeyslop.Reports;

namespace Honeyslop.Admin.Core.Services.Internals;

internal sealed class BlockedContactsService(
    IBlockedAbuseContactRepository repository,
    IAccessGuard access,
    IServerTimeRepository serverTime) : IBlockedContactsService
{
    private const string Contacts = "Contacts";

    public async Task<BlockedContactsPageDto> GetPageAsync(BlockedContactsQuery query, CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        var page = Math.Max(0, query.Page);
        var items = await repository.GetPageAsync(query.Search, page, IBlockedContactsService.PageSize, cancellationToken);
        var total = await repository.CountAsync(query.Search, cancellationToken);
        var canOperate = access.Viewer.CanOperate;
        return new BlockedContactsPageDto(
            new PagedDto<BlockedContactDto>(
                [.. items.Select(x => new BlockedContactDto(x.Contact, x.Note, canOperate ? x.CreatedBy : null, x.CreatedAt))],
                total,
                page,
                IBlockedContactsService.PageSize),
            await serverTime.GetNowAsync(cancellationToken));
    }

    public async Task<ContactsChangeDto> BlockAsync(IReadOnlyCollection<string> contacts, string? note, CancellationToken cancellationToken)
    {
        access.EnsureCanOperate();
        var entries = AbuseContactBlocklist.Parse(contacts);
        var cleanNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (Problem(entries) is { } problem)
            return Refused(Contacts, problem);
        if (cleanNote?.Length > BlockedAbuseContact.NoteMaxLength)
            return Refused("Note", $"Must be at most {BlockedAbuseContact.NoteMaxLength} characters.");

        var now = await serverTime.GetNowAsync(cancellationToken);
        var added = await repository.AddAsync(
            [
                .. entries.Select(x => new BlockedAbuseContact
                {
                    Contact = x,
                    Note = cleanNote,
                    CreatedBy = access.Viewer.Name,
                    CreatedAt = now
                })
            ],
            cancellationToken);
        return new ContactsChangeDto(new Dictionary<string, string>(), added);
    }

    public async Task<ContactsChangeDto> UnblockAsync(IReadOnlyCollection<string> contacts, CancellationToken cancellationToken)
    {
        access.EnsureCanOperate();
        var entries = AbuseContactBlocklist.Parse(contacts);
        if (entries.Length == 0)
            return Refused(Contacts, "Name at least one address or @domain.");
        return new ContactsChangeDto(new Dictionary<string, string>(), await repository.RemoveAsync(entries, cancellationToken));
    }

    private static string? Problem(string[] entries)
    {
        if (entries.Length == 0)
            return "Name at least one address or @domain.";
        if (entries.Length > IBlockedContactsService.MaxPerChange)
            return $"At most {IBlockedContactsService.MaxPerChange} at a time.";
        if (entries.FirstOrDefault(x => x.Length > BlockedAbuseContact.ContactMaxLength) is { } tooLong)
            return $"\"{tooLong}\" is longer than {BlockedAbuseContact.ContactMaxLength} characters.";
        return AbuseContactBlocklist.Problem(entries);
    }

    private static ContactsChangeDto Refused(string field, string error)
    {
        return new ContactsChangeDto(new Dictionary<string, string>
        {
            [field] = error
        }, 0);
    }
}