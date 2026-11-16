using System.Security.Cryptography;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Tests;

namespace Honeyslop.Data.Tests;

[TestClass]
[TestCategory("Integration")]
public sealed class EntryRepositoryTests
{
    [TestMethod]
    public async Task TryAddAsync_SameKeyTwice_OnlyFirstWins()
    {
        var keyHash = RandomNumberGenerator.GetBytes(32);
        await using var firstScope = PostgresDatabase.CreateScope();
        await using var secondScope = PostgresDatabase.CreateScope();

        var first = await firstScope.Repository().TryAddAsync(PendingEntry(keyHash), CancellationToken.None);
        var second = await secondScope.Repository().TryAddAsync(PendingEntry(keyHash), CancellationToken.None);

        Assert.IsTrue(first);
        Assert.IsFalse(second);
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task TryClaimNextAsync_ConcurrentWorkers_EachEntryIsClaimedOnce()
    {
        var queued = new List<long>();
        for (var index = 0; index < 5; index++)
            queued.Add((await AddAsync(QueuedEntry())).Id);
        var now = DateTimeOffset.UtcNow;

        var claimed = await Task.WhenAll(Enumerable.Range(0, 40).Select(async index =>
        {
            await using var scope = PostgresDatabase.CreateScope();
            return await scope.Repository().TryClaimNextAsync($"worker-{index}", now, now.AddMinutes(-1), now.AddMinutes(-15), now.AddMinutes(-1), 3, CancellationToken.None);
        }));

        var ids = claimed.OfType<long>().ToArray();
        Assert.AreEqual(ids.Length, ids.Distinct().Count());
        foreach (var id in queued)
        {
            var entry = await GetAsync(id);
            Assert.IsNotNull(entry.ClaimedBy);
            CollectionAssert.Contains(ids, id);
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task TryClaimNextAsync_PrefersFreshlyRequestedEntriesThenTheOldest()
    {
        var future = DateTimeOffset.UtcNow.AddYears(100);
        await using (var drain = PostgresDatabase.CreateScope())
            while (await drain.Repository().TryClaimNextAsync("drain", future, future, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, 3, CancellationToken.None) is not null)
            {
            }

        var oldest = await AddAsync(QueuedEntry(future));
        var older = await AddAsync(QueuedEntry(future.AddMinutes(1)));
        var retried = await AddAsync(QueuedEntry(future.AddMinutes(2)));
        var fresh = await AddAsync(QueuedEntry(future.AddMinutes(30)));
        await using var scope = PostgresDatabase.CreateScope();
        await scope.Repository().TouchRequestedAsync(retried.Id, future.AddMinutes(29), CancellationToken.None);

        var claimed = new List<long?>();
        for (var index = 0; index < 4; index++)
            claimed.Add(await scope.Repository().TryClaimNextAsync("worker", future.AddMinutes(30), future.AddMinutes(29), DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, 3, CancellationToken.None));

        CollectionAssert.AreEqual(new long?[] { fresh.Id, retried.Id, oldest.Id, older.Id }, claimed);
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task TryClaimNextAsync_TakesOverStaleClaimsAndDueRetriesOnly()
    {
        var future = DateTimeOffset.UtcNow.AddYears(60);
        var stale = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), createdAt: future, claimedAt: future.AddHours(-1)));
        var busy = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), createdAt: future.AddSeconds(1), claimedAt: future));
        var retry = await AddAsync(FailedEntry(future.AddSeconds(2), 1, future.AddMinutes(-5)));
        var exhausted = await AddAsync(FailedEntry(future.AddSeconds(3), 3, future.AddMinutes(-5)));
        var recentFailure = await AddAsync(FailedEntry(future.AddSeconds(4), 1, future));

        var claimed = new List<long>();
        await using (var scope = PostgresDatabase.CreateScope())
            while (await scope.Repository().TryClaimNextAsync("worker", future, future.AddMinutes(-1), future.AddMinutes(-15), future.AddMinutes(-1), 3, CancellationToken.None) is { } id)
                claimed.Add(id);

        CollectionAssert.Contains(claimed, stale.Id);
        CollectionAssert.Contains(claimed, retry.Id);
        CollectionAssert.DoesNotContain(claimed, busy.Id);
        CollectionAssert.DoesNotContain(claimed, exhausted.Id);
        CollectionAssert.DoesNotContain(claimed, recentFailure.Id);
        Assert.AreEqual(EntryStatus.Pending, (await GetAsync(retry.Id)).Status);
    }

    [TestMethod]
    public async Task GetFinishedAsync_ReturnsOnlyReadyAndFailedEntries()
    {
        var pending = await AddAsync(QueuedEntry());
        var ready = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), EntryStatus.Ready));
        var failed = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), EntryStatus.Failed));
        await using var scope = PostgresDatabase.CreateScope();

        var finished = await scope.Repository().GetFinishedAsync([pending.Id, ready.Id, failed.Id], CancellationToken.None);

        CollectionAssert.AreEquivalent(new[] { ready.Id, failed.Id }, finished.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task CompleteAsync_StoresTheResponse()
    {
        var entry = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32)));
        await using (var scope = PostgresDatabase.CreateScope())
            await scope.Repository().CompleteAsync(
                entry.Id,
                "test",
                new EntryResponse(
                    401,
                    [
                        new ResponseHeader
                        {
                            Name = "Set-Cookie",
                            Value = "a=1"
                        },
                        new ResponseHeader
                        {
                            Name = "Set-Cookie",
                            Value = "b=2"
                        }
                    ],
                    "<h1>Unauthorized</h1>",
                    "honeyslop",
                    300,
                    120,
                    4200),
                DateTimeOffset.UtcNow,
                CancellationToken.None);

        var completed = await GetByKeyAsync(entry.KeyHash);

        Assert.AreEqual(EntryStatus.Ready, completed.Status);
        Assert.AreEqual(401, completed.ResponseStatus);
        Assert.AreEqual("<h1>Unauthorized</h1>", completed.ResponseBody);
        CollectionAssert.AreEqual(new[] { "a=1", "b=2" }, completed.ResponseHeaders.Select(x => x.Value).ToArray());
        Assert.AreEqual(120, completed.CompletionTokens);
        Assert.IsNotNull(completed.GeneratedAt);
    }

    [TestMethod]
    public async Task CompleteAsync_ByAnotherInstance_ChangesNothing()
    {
        var entry = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32)));
        await using var scope = PostgresDatabase.CreateScope();

        var stored = await scope.Repository().CompleteAsync(entry.Id, "someone-else", new EntryResponse(200, [], "late", null, null, null, 1), DateTimeOffset.UtcNow, CancellationToken.None);
        await scope.Repository().FailAsync(entry.Id, "someone-else", "late", DateTimeOffset.UtcNow, CancellationToken.None);
        await scope.Repository().RequeueAsync(entry.Id, "someone-else", "late", DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.IsFalse(stored);
        var unchanged = await GetAsync(entry.Id);
        Assert.AreEqual(EntryStatus.Pending, unchanged.Status);
        Assert.AreEqual("test", unchanged.ClaimedBy);
        Assert.AreEqual(0, unchanged.Attempts);
    }

    [TestMethod]
    public async Task HeartbeatAsync_KeepsTheClaimFreshForItsOwnerOnly()
    {
        var entry = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), claimedAt: DateTimeOffset.UtcNow.AddHours(-1)));
        var now = DateTimeOffset.UtcNow;
        await using var scope = PostgresDatabase.CreateScope();

        var foreign = await scope.Repository().HeartbeatAsync(entry.Id, "someone-else", now, CancellationToken.None);
        var own = await scope.Repository().HeartbeatAsync(entry.Id, "test", now, CancellationToken.None);

        Assert.IsFalse(foreign);
        Assert.IsTrue(own);
        Assert.AreEqual(now.ToUnixTimeMilliseconds(), (await GetAsync(entry.Id)).ClaimedAt!.Value.ToUnixTimeMilliseconds());
    }

    [TestMethod]
    public async Task RequeueAsync_ByTheOwner_PutsTheEntryBackWithoutAnAttempt()
    {
        var entry = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32)));
        await using var scope = PostgresDatabase.CreateScope();

        await scope.Repository().RequeueAsync(entry.Id, "test", "server gone", DateTimeOffset.UtcNow, CancellationToken.None);

        var requeued = await GetAsync(entry.Id);
        Assert.AreEqual(EntryStatus.Pending, requeued.Status);
        Assert.IsNull(requeued.ClaimedBy);
        Assert.IsNull(requeued.ClaimedAt);
        Assert.AreEqual(0, requeued.Attempts);
        Assert.AreEqual("server gone", requeued.Error);
    }

    [TestMethod]
    public async Task FailAsync_CountsTheAttemptAndClosesTheClaim()
    {
        var entry = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32)));
        await using (var scope = PostgresDatabase.CreateScope())
        {
            await scope.Repository().FailAsync(entry.Id, "test", "boom", DateTimeOffset.UtcNow, CancellationToken.None);
            await scope.Repository().FailAsync(entry.Id, "test", "again", DateTimeOffset.UtcNow, CancellationToken.None);
        }

        var failed = await GetAsync(entry.Id);

        Assert.AreEqual(EntryStatus.Failed, failed.Status);
        Assert.AreEqual(1, failed.Attempts);
        Assert.AreEqual("boom", failed.Error);
    }

    [TestMethod]
    public async Task AddHitsAsync_Accumulates()
    {
        var entry = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32)));
        var lastHitAt = DateTimeOffset.UtcNow;
        await using (var scope = PostgresDatabase.CreateScope())
        {
            await scope.Repository().AddHitsAsync(entry.Id, 3, lastHitAt.AddMinutes(-1), CancellationToken.None);
            await scope.Repository().AddHitsAsync(entry.Id, 2, lastHitAt, CancellationToken.None);
        }

        var hit = await GetAsync(entry.Id);

        Assert.AreEqual(5, hit.Hits);
        Assert.AreEqual(lastHitAt.ToUnixTimeMilliseconds(), hit.LastHitAt!.Value.ToUnixTimeMilliseconds());
    }

    [TestMethod]
    public async Task DeleteAsync_ByFilter_DeletesOnlyMatchingEntries()
    {
        var host = Unique.Host();
        var matching = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), host: host, target: "/wp-admin/install.php"));
        var otherPath = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), host: host, target: "/.env"));
        var otherHost = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), target: "/wp-admin/install.php"));

        int deleted;
        int counted;
        await using (var scope = PostgresDatabase.CreateScope())
        {
            var filter = new EntryFilter(host.ToUpperInvariant(), "WP-ADMIN");
            counted = await scope.Repository().CountAsync(filter, CancellationToken.None);
            deleted = await scope.Repository().DeleteAsync(filter, CancellationToken.None);
        }

        Assert.AreEqual(1, counted);
        Assert.AreEqual(1, deleted);
        Assert.IsNull(await FindAsync(matching.Id));
        Assert.IsNotNull(await FindAsync(otherPath.Id));
        Assert.IsNotNull(await FindAsync(otherHost.Id));
    }

    [TestMethod]
    public async Task DeleteAsync_FilterWithLikeWildcards_MatchesThemLiterally()
    {
        var host = Unique.Host();
        var literal = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), host: host, target: "/100%_done"));
        var wildcardMatch = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), host: host, target: "/100xxdone"));

        await using (var scope = PostgresDatabase.CreateScope())
            await scope.Repository().DeleteAsync(new EntryFilter(host, "100%_"), CancellationToken.None);

        Assert.IsNull(await FindAsync(literal.Id));
        Assert.IsNotNull(await FindAsync(wildcardMatch.Id));
    }

    [TestMethod]
    public async Task DeleteUnusedSinceAsync_KeepsRecentAndPendingEntries()
    {
        var now = DateTimeOffset.UtcNow;
        var old = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), EntryStatus.Ready, createdAt: now.AddDays(-40)));
        var oldButHit = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), EntryStatus.Ready, createdAt: now.AddDays(-40)));
        var oldPending = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), createdAt: now.AddDays(-40)));
        await using (var scope = PostgresDatabase.CreateScope())
        {
            await scope.Repository().AddHitsAsync(oldButHit.Id, 1, now, CancellationToken.None);
            await scope.Repository().DeleteUnusedSinceAsync(now.AddDays(-30), CancellationToken.None);
        }

        Assert.IsNull(await FindAsync(old.Id));
        Assert.IsNotNull(await FindAsync(oldButHit.Id));
        Assert.IsNotNull(await FindAsync(oldPending.Id));
    }

    [TestMethod]
    public async Task GetGeneratorStatisticsAsync_GroupsReadyEntriesByModelServer()
    {
        var model = $"honeyslop@{Unique.String(8)}:8080";
        foreach (var (ms, tokens) in new[] { (1000, 100), (3000, 300) })
        {
            var entry = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32)));
            await using var scope = PostgresDatabase.CreateScope();
            await scope.Repository().CompleteAsync(entry.Id, "test", new EntryResponse(200, [], "ok", model, 10, tokens, ms), DateTimeOffset.UtcNow, CancellationToken.None);
        }

        await using var readScope = PostgresDatabase.CreateScope();
        var statistics = (await readScope.Repository().GetGeneratorStatisticsAsync(DateTimeOffset.UtcNow.AddHours(-1), CancellationToken.None))
            .Single(x => x.Model == model);
        var queue = await readScope.Repository().GetQueueStatisticsAsync(CancellationToken.None);

        Assert.AreEqual(2, statistics.Generated);
        Assert.AreEqual(2000, statistics.AverageMs, 0.1);
        Assert.AreEqual(200, statistics.AverageTokens, 0.1);
        Assert.IsNotNull(statistics.LastGeneratedAt);
        Assert.IsGreaterThanOrEqualTo(0, queue.Queued);
    }

    [TestMethod]
    public async Task GetPageAsync_FiltersAndProjectsSummaries()
    {
        var host = Unique.Host();
        var entry = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), host: host, target: "/phpinfo.php"));
        await using var scope = PostgresDatabase.CreateScope();

        var page = await scope.Repository().GetPageAsync(new EntryFilter(host), EntrySort.MostHits, 0, 10, CancellationToken.None);
        var statistics = await scope.Repository().GetStatisticsAsync(CancellationToken.None);

        Assert.AreEqual(1, page.Length);
        Assert.AreEqual(entry.Id, page[0].Id);
        Assert.AreEqual("/phpinfo.php", page[0].Target);
        Assert.AreEqual(0, page[0].BodyLength);
        Assert.IsGreaterThanOrEqualTo(1, statistics.Generating);
    }

    private static Entry PendingEntry(
        byte[] keyHash,
        EntryStatus status = EntryStatus.Pending,
        string? host = null,
        string? target = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? claimedAt = null)
    {
        var now = createdAt ?? DateTimeOffset.UtcNow;
        return new Entry
        {
            KeyHash = keyHash,
            Method = "GET",
            Host = host ?? Unique.Host(),
            Target = target ?? Unique.Target(),
            Status = status,
            ClaimedBy = "test",
            ClaimedAt = claimedAt ?? now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static Entry QueuedEntry(DateTimeOffset? createdAt = null)
    {
        var entry = PendingEntry(RandomNumberGenerator.GetBytes(32), createdAt: createdAt);
        entry.ClaimedBy = null;
        entry.ClaimedAt = null;
        entry.RequestedAt = entry.CreatedAt;
        return entry;
    }

    private static Entry FailedEntry(DateTimeOffset createdAt, int attempts, DateTimeOffset updatedAt)
    {
        var entry = PendingEntry(RandomNumberGenerator.GetBytes(32), EntryStatus.Failed, createdAt: createdAt);
        entry.Attempts = attempts;
        entry.UpdatedAt = updatedAt;
        return entry;
    }

    private static async Task<Entry> AddAsync(Entry entry)
    {
        await using var scope = PostgresDatabase.CreateScope();
        Assert.IsTrue(await scope.Repository().TryAddAsync(entry, CancellationToken.None));
        return await scope.Repository().GetByIdOrDefaultAsync(entry.Id, CancellationToken.None) ?? throw new AssertFailedException("Entry was not stored.");
    }

    private static async Task<Entry?> FindAsync(long id)
    {
        await using var scope = PostgresDatabase.CreateScope();
        return await scope.Repository().GetByIdOrDefaultAsync(id, CancellationToken.None);
    }

    private static async Task<Entry> GetAsync(long id)
    {
        return await FindAsync(id) ?? throw new AssertFailedException($"Entry {id} not found.");
    }

    private static async Task<Entry> GetByKeyAsync(byte[] keyHash)
    {
        await using var scope = PostgresDatabase.CreateScope();
        return await scope.Repository().GetByKeyHashOrDefaultAsync(keyHash, CancellationToken.None)
               ?? throw new AssertFailedException("Entry not found by key.");
    }
}
