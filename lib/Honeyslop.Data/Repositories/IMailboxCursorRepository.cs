using Honeyslop.Data.Entities;

namespace Honeyslop.Data.Repositories;

public interface IMailboxCursorRepository
{
    Task<MailboxCursor?> GetOrDefaultAsync(string folder, CancellationToken cancellationToken);
    Task SaveAsync(string folder, long uidValidity, long lastUid, DateTimeOffset now, CancellationToken cancellationToken);
}