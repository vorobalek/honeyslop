using Honeyslop.Data.Entities;
using Honeyslop.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class MailboxCursorRepository(ApplicationDbContext dbContext) : IMailboxCursorRepository
{
    public Task<MailboxCursor?> GetOrDefaultAsync(string folder, CancellationToken cancellationToken)
    {
        return dbContext
            .MailboxCursors
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Folder == folder, cancellationToken);
    }

    public async Task SaveAsync(string folder, long uidValidity, long lastUid, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (await MoveAsync(folder, uidValidity, lastUid, now, cancellationToken) > 0)
            return;

        if (!await dbContext.MailboxCursors.AnyAsync(x => x.Folder == folder, cancellationToken))
        {
            var cursor = new MailboxCursor
            {
                Folder = folder,
                UidValidity = uidValidity,
                LastUid = lastUid,
                UpdatedAt = now
            };
            await dbContext.MailboxCursors.AddAsync(cursor, cancellationToken);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateException exception) when (exception.IsUniqueViolation)
            {
                dbContext.Entry(cursor).State = EntityState.Detached;
            }
        }

        await MoveAsync(folder, uidValidity, lastUid, now, cancellationToken);
    }

    private Task<int> MoveAsync(string folder, long uidValidity, long lastUid, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return dbContext
            .MailboxCursors
            .Where(x => x.Folder == folder && (x.UidValidity != uidValidity || x.LastUid < lastUid))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.UidValidity, uidValidity)
                    .SetProperty(x => x.LastUid, lastUid)
                    .SetProperty(x => x.UpdatedAt, now),
                cancellationToken);
    }
}