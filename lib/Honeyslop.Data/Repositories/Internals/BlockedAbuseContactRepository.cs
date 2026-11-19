using Honeyslop.Data.Entities;
using Honeyslop.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Honeyslop.Data.Repositories.Internals;

internal sealed class BlockedAbuseContactRepository(ApplicationDbContext dbContext) : IBlockedAbuseContactRepository
{
    public Task<BlockedAbuseContact[]> GetAnyOfAsync(IReadOnlyCollection<string> contacts, CancellationToken cancellationToken)
    {
        return dbContext
            .BlockedAbuseContacts
            .AsNoTracking()
            .Where(x => contacts.Contains(x.Contact))
            .ToArrayAsync(cancellationToken);
    }

    public Task<BlockedAbuseContact[]> GetPageAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        return Matching(search)
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Contact)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountAsync(string? search, CancellationToken cancellationToken)
    {
        return Matching(search).CountAsync(cancellationToken);
    }

    public async Task<int> AddAsync(IReadOnlyCollection<BlockedAbuseContact> contacts, CancellationToken cancellationToken)
    {
        var keys = contacts.Select(x => x.Contact).ToArray();
        var existing = await dbContext
            .BlockedAbuseContacts
            .Where(x => keys.Contains(x.Contact))
            .Select(x => x.Contact)
            .ToArrayAsync(cancellationToken);
        BlockedAbuseContact[] missing = [.. contacts.Where(x => !existing.Contains(x.Contact))];
        if (missing.Length == 0)
            return 0;

        await dbContext.BlockedAbuseContacts.AddRangeAsync(missing, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return missing.Length;
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation)
        {
            dbContext.ChangeTracker.Clear();
            return await AddOneByOneAsync(missing, cancellationToken);
        }
    }

    public Task<int> RemoveAsync(IReadOnlyCollection<string> contacts, CancellationToken cancellationToken)
    {
        return dbContext
            .BlockedAbuseContacts
            .Where(x => contacts.Contains(x.Contact))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<int> AddOneByOneAsync(IReadOnlyCollection<BlockedAbuseContact> contacts, CancellationToken cancellationToken)
    {
        var added = 0;
        foreach (var contact in contacts)
        {
            await dbContext.BlockedAbuseContacts.AddAsync(contact, cancellationToken);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                added++;
            }
            catch (DbUpdateException exception) when (exception.IsUniqueViolation)
            {
                dbContext.Entry(contact).State = EntityState.Detached;
            }
        }

        return added;
    }

    private IQueryable<BlockedAbuseContact> Matching(string? search)
    {
        var query = dbContext.BlockedAbuseContacts.AsNoTracking();
        if (string.IsNullOrWhiteSpace(search))
            return query;
        var pattern = search.Trim().ContainsPattern;
        return query.Where(x =>
            EF.Functions.ILike(x.Contact, pattern, LikePatterns.Escape) ||
            x.Note != null && EF.Functions.ILike(x.Note, pattern, LikePatterns.Escape));
    }
}