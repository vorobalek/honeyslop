using Honeyslop.Data.Repositories;
using Honeyslop.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Data.Tests;

[TestClass]
[TestCategory("Integration")]
public sealed class MailboxCursorRepositoryTests
{
    [TestMethod]
    public async Task SaveAsync_OnlyMovesForward_UntilTheFolderIsRecreated()
    {
        var folder = $"INBOX/{Unique.String(8)}";
        var now = DateTimeOffset.UtcNow;

        var missing = await GetAsync(folder);
        await SaveAsync(folder, 7, 10, now);
        await SaveAsync(folder, 7, 4, now);
        var kept = await GetAsync(folder);
        await SaveAsync(folder, 7, 12, now);
        var moved = await GetAsync(folder);
        await SaveAsync(folder, 8, 2, now);
        var recreated = await GetAsync(folder);

        Assert.IsNull(missing);
        Assert.AreEqual((7L, 10L), (kept!.UidValidity, kept.LastUid));
        Assert.AreEqual((7L, 12L), (moved!.UidValidity, moved.LastUid));
        Assert.AreEqual((8L, 2L), (recreated!.UidValidity, recreated.LastUid));
    }

    [TestMethod]
    public async Task SaveAsync_FromTwoReplicasAtOnce_KeepsTheFurthestPosition()
    {
        var folder = $"INBOX/{Unique.String(8)}";
        var now = DateTimeOffset.UtcNow;

        await Task.WhenAll(Enumerable.Range(1, 8).Select(x => SaveAsync(folder, 3, x, now)));

        Assert.AreEqual(8L, (await GetAsync(folder))!.LastUid);
    }

    private static async Task<Entities.MailboxCursor?> GetAsync(string folder)
    {
        await using var scope = PostgresDatabase.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IMailboxCursorRepository>().GetOrDefaultAsync(folder, CancellationToken.None);
    }

    private static async Task SaveAsync(string folder, long uidValidity, long lastUid, DateTimeOffset now)
    {
        await using var scope = PostgresDatabase.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMailboxCursorRepository>().SaveAsync(folder, uidValidity, lastUid, now, CancellationToken.None);
    }
}