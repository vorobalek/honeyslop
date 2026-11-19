using Honeyslop.Data.Entities;
using Honeyslop.Data.Repositories;
using Honeyslop.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Data.Tests;

[TestClass]
[TestCategory("Integration")]
public sealed class BlockedAbuseContactRepositoryTests
{
    [TestMethod]
    public async Task AddAsync_KeepsWhatIsBlockedAlreadyAndAddsTheRest()
    {
        var host = $"{Unique.String(8)}.example";
        var first = Contact($"abuse@{host}", "bounces");

        var added = new[]
        {
            await AddAsync([first]),
            await AddAsync([Contact($"abuse@{host}", "again"), Contact($"@{host}", null)])
        };

        CollectionAssert.AreEqual(new[] { 1, 1 }, added);
        await using var scope = PostgresDatabase.CreateScope();
        var stored = await Repository(scope).GetAnyOfAsync([$"abuse@{host}", $"@{host}", "abuse@other.example"], CancellationToken.None);
        CollectionAssert.AreEquivalent(new[] { $"abuse@{host}", $"@{host}" }, stored.Select(x => x.Contact).ToArray());
        Assert.AreEqual("bounces", stored.Single(x => x.Contact == $"abuse@{host}").Note);
    }

    [TestMethod]
    public async Task GetPageAsync_FindsByAddressOrNote_AndRemoveAsyncTakesThemOut()
    {
        var host = $"{Unique.String(8)}.example";
        await AddAsync([Contact($"abuse@{host}", null), Contact($"@mail.{host}", "answers with bounces"), Contact("@unrelated.example", null)]);
        await using var scope = PostgresDatabase.CreateScope();
        var repository = Repository(scope);

        var found = await repository.GetPageAsync(host, 0, 50, CancellationToken.None);
        var byNote = await repository.CountAsync("answers with bounces", CancellationToken.None);
        var removed = await repository.RemoveAsync([$"abuse@{host}", $"@mail.{host}"], CancellationToken.None);

        Assert.HasCount(2, found);
        Assert.IsGreaterThanOrEqualTo(1, byNote);
        Assert.AreEqual(2, removed);
        Assert.AreEqual(0, await repository.CountAsync(host, CancellationToken.None));
    }

    private static BlockedAbuseContact Contact(string contact, string? note)
    {
        return new BlockedAbuseContact
        {
            Contact = contact,
            Note = note,
            CreatedBy = "operator",
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static async Task<int> AddAsync(BlockedAbuseContact[] contacts)
    {
        await using var scope = PostgresDatabase.CreateScope();
        return await Repository(scope).AddAsync(contacts, CancellationToken.None);
    }

    private static IBlockedAbuseContactRepository Repository(AsyncServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<IBlockedAbuseContactRepository>();
    }
}