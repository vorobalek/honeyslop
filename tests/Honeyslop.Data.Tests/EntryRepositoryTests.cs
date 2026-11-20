using System.Security.Cryptography;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Tests;
using Microsoft.Extensions.DependencyInjection;

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
        {
            while (await drain.Repository().TryClaimNextAsync("drain", future, future, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, 3, CancellationToken.None) is not null)
            {
            }
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

        CollectionAssert.AreEqual(new long?[]
        {
            fresh.Id, retried.Id, oldest.Id, older.Id
        }, claimed);
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
        {
            while (await scope.Repository().TryClaimNextAsync("worker", future, future.AddMinutes(-1), future.AddMinutes(-15), future.AddMinutes(-1), 3, CancellationToken.None) is { } id)
                claimed.Add(id);
        }

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

        CollectionAssert.AreEquivalent(new[]
        {
            ready.Id, failed.Id
        }, finished.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task CompleteAsync_StoresTheResponse()
    {
        var entry = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32)));
        await using (var scope = PostgresDatabase.CreateScope())
        {
            await scope.Repository().CompleteAsync(
                new Entry
                {
                    Id = entry.Id,
                    ResponseStatus = 401,
                    ResponseHeaders =
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
                    ResponseBody = "<h1>Unauthorized</h1>",
                    Model = "honeyslop",
                    Generator = "llm-0",
                    PromptTokens = 300,
                    CompletionTokens = 120,
                    GenerationMs = 4200
                },
                "test",
                DateTimeOffset.UtcNow,
                CancellationToken.None);
        }

        var completed = await GetByKeyAsync(entry.KeyHash);

        Assert.AreEqual(EntryStatus.Ready, completed.Status);
        Assert.AreEqual(401, completed.ResponseStatus);
        Assert.AreEqual("<h1>Unauthorized</h1>", completed.ResponseBody);
        CollectionAssert.AreEqual(new[]
        {
            "a=1", "b=2"
        }, completed.ResponseHeaders.Select(x => x.Value).ToArray());
        Assert.AreEqual(120, completed.CompletionTokens);
        Assert.AreEqual("honeyslop", completed.Model);
        Assert.AreEqual("llm-0", completed.Generator);
        Assert.IsNotNull(completed.GeneratedAt);
    }

    [TestMethod]
    public async Task CompleteAsync_ByAnotherInstance_ChangesNothing()
    {
        var entry = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32)));
        await using var scope = PostgresDatabase.CreateScope();

        var stored = await scope.Repository().CompleteAsync(new Entry
        {
            Id = entry.Id,
            ResponseStatus = 200,
            ResponseBody = "late",
            GenerationMs = 1
        }, "someone-else", DateTimeOffset.UtcNow, CancellationToken.None);
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
    public async Task MarkDispatchedAsync_RecordsTheGeneratorForTheOwnerOnly()
    {
        var foreign = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32)));
        var own = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32)));
        await using var scope = PostgresDatabase.CreateScope();

        await scope.Repository().MarkDispatchedAsync(foreign.Id, "someone-else", "llm-0", CancellationToken.None);
        await scope.Repository().MarkDispatchedAsync(own.Id, "test", "llm-1", CancellationToken.None);

        Assert.IsNull((await GetAsync(foreign.Id)).Generator);
        Assert.AreEqual("llm-1", (await GetAsync(own.Id)).Generator);
        var generating = await scope.Repository().GetGeneratingAsync(1000, CancellationToken.None);
        Assert.AreEqual("llm-1", generating.Single(x => x.Id == own.Id).Generator);
    }

    [TestMethod]
    public async Task RequeueAsync_ByTheOwner_PutsTheEntryBackWithoutAnAttempt()
    {
        var entry = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32)));
        await using var scope = PostgresDatabase.CreateScope();
        await scope.Repository().MarkDispatchedAsync(entry.Id, "test", "llm-0", CancellationToken.None);

        await scope.Repository().RequeueAsync(entry.Id, "test", "server gone", DateTimeOffset.UtcNow, CancellationToken.None);

        var requeued = await GetAsync(entry.Id);
        Assert.AreEqual(EntryStatus.Pending, requeued.Status);
        Assert.IsNull(requeued.ClaimedBy);
        Assert.IsNull(requeued.ClaimedAt);
        Assert.IsNull(requeued.Generator);
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
    public async Task ExpireAsync_ByFilter_ExpiresOnlyMatchingEntries()
    {
        var host = Unique.Host();
        var matching = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), host: host, target: "/wp-admin/install.php"));
        var otherPath = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), host: host, target: "/.env"));
        var otherHost = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), target: "/wp-admin/install.php"));

        int expired;
        int counted;
        await using (var scope = PostgresDatabase.CreateScope())
        {
            counted = await scope.Repository().CountAsync(host.ToUpperInvariant(), "WP-ADMIN", null, CancellationToken.None);
            expired = await scope.Repository().ExpireMatchingAsync(host.ToUpperInvariant(), "WP-ADMIN", null, DateTimeOffset.UtcNow, CancellationToken.None);
        }

        Assert.AreEqual(1, counted);
        Assert.AreEqual(1, expired);
        Assert.AreEqual(EntryStatus.Expired, (await GetAsync(matching.Id)).Status);
        Assert.AreEqual(EntryStatus.Pending, (await GetAsync(otherPath.Id)).Status);
        Assert.AreEqual(EntryStatus.Pending, (await GetAsync(otherHost.Id)).Status);
    }

    [TestMethod]
    public async Task ExpireAsync_FilterWithLikeWildcards_MatchesThemLiterally()
    {
        var host = Unique.Host();
        var literal = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), host: host, target: "/100%_done"));
        var wildcardMatch = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), host: host, target: "/100xxdone"));

        await using (var scope = PostgresDatabase.CreateScope())
        {
            await scope.Repository().ExpireMatchingAsync(host, "100%_", null, DateTimeOffset.UtcNow, CancellationToken.None);
        }

        Assert.AreEqual(EntryStatus.Expired, (await GetAsync(literal.Id)).Status);
        Assert.AreEqual(EntryStatus.Pending, (await GetAsync(wildcardMatch.Id)).Status);
    }

    [TestMethod]
    public async Task ExpireAsync_KeepsTheEntryAndFreesItsPath()
    {
        var host = Unique.Host();
        var keyHash = RandomNumberGenerator.GetBytes(32);
        var old = await AddAsync(PendingEntry(keyHash, EntryStatus.Ready, host));

        await using (var scope = PostgresDatabase.CreateScope())
        {
            Assert.AreEqual(1, await scope.Repository().ExpireAsync([old.Id], DateTimeOffset.UtcNow, CancellationToken.None));
        }

        await using (var scope = PostgresDatabase.CreateScope())
        {
            Assert.IsNull(await scope.Repository().GetByKeyHashOrDefaultAsync(keyHash, CancellationToken.None));
        }

        var current = await AddAsync(PendingEntry(keyHash, host: host));

        await using var readScope = PostgresDatabase.CreateScope();
        Assert.AreEqual(current.Id, (await GetByKeyAsync(keyHash)).Id);
        Assert.AreEqual(EntryStatus.Expired, (await GetAsync(old.Id)).Status);
        CollectionAssert.AreEqual(
            new[]
            {
                current.Id
            },
            (await readScope.Repository().GetPageAsync(host, null, null, EntryOrder.Id, SortDirection.Desc, 0, 10, CancellationToken.None)).Select(x => x.Id).ToArray());
        CollectionAssert.AreEqual(
            new[]
            {
                old.Id
            },
            (await readScope.Repository().GetPageAsync(host, null, EntryState.Expired, EntryOrder.Id, SortDirection.Desc, 0, 10, CancellationToken.None)).Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task ExpireUnusedSinceAsync_KeepsRecentAndPendingEntries()
    {
        var now = DateTimeOffset.UtcNow;
        var old = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), EntryStatus.Ready, createdAt: now.AddDays(-40)));
        var oldButHit = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), EntryStatus.Ready, createdAt: now.AddDays(-40)));
        var oldPending = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), createdAt: now.AddDays(-40)));
        await using (var scope = PostgresDatabase.CreateScope())
        {
            await scope.Repository().AddHitsAsync(oldButHit.Id, 1, now, CancellationToken.None);
            await scope.Repository().ExpireUnusedSinceAsync(now.AddDays(-30), now, CancellationToken.None);
        }

        Assert.AreEqual(EntryStatus.Expired, (await GetAsync(old.Id)).Status);
        Assert.AreEqual(EntryStatus.Ready, (await GetAsync(oldButHit.Id)).Status);
        Assert.AreEqual(EntryStatus.Pending, (await GetAsync(oldPending.Id)).Status);
    }

    [TestMethod]
    public async Task AnyFromClientAsync_FindsEntriesByTheirFirstClient()
    {
        var clientIp = $"198.51.100.{Random.Shared.Next(1, 254)}-{Unique.String(6)}";
        var entry = PendingEntry(RandomNumberGenerator.GetBytes(32), EntryStatus.Ready);
        entry.ClientIp = clientIp;
        await AddAsync(entry);

        await using var scope = PostgresDatabase.CreateScope();
        Assert.IsTrue(await scope.Repository().AnyFromClientAsync(clientIp, CancellationToken.None));
        Assert.IsFalse(await scope.Repository().AnyFromClientAsync(clientIp + "-unknown", CancellationToken.None));
    }

    [TestMethod]
    public async Task DeleteExpiredUnreferencedAsync_KeepsEntriesThatRequestsOrStatisticsStillNeed()
    {
        var now = DateTimeOffset.UtcNow;
        var referenced = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), EntryStatus.Ready));
        var recent = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), EntryStatus.Ready));
        var forgotten = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), EntryStatus.Ready));
        await using (var scope = PostgresDatabase.CreateScope())
        {
            await scope.Repository().ExpireAsync([referenced.Id, forgotten.Id], now.AddDays(-2), CancellationToken.None);
            await scope.Repository().ExpireAsync([recent.Id], now, CancellationToken.None);
            await scope.ServiceProvider.GetRequiredService<IRequestRepository>().AddRangeAsync(
                [
                    new RequestRecord
                    {
                        ReceivedAt = now,
                        ClientIp = "203.0.113.7",
                        Method = "GET",
                        Host = referenced.Host,
                        Target = referenced.Target,
                        Headers = string.Empty,
                        Outcome = RequestOutcome.Hit,
                        EntryId = referenced.Id,
                        Instance = "test"
                    }
                ],
                CancellationToken.None);
            await scope.Repository().DeleteExpiredUnreferencedAsync(now.AddDays(-1), CancellationToken.None);
        }

        Assert.IsNotNull(await FindAsync(referenced.Id));
        Assert.IsNotNull(await FindAsync(recent.Id));
        Assert.IsNull(await FindAsync(forgotten.Id));
    }

    [TestMethod]
    public async Task GetGeneratorStatisticsAsync_GroupsGeneratedEntriesByModelServerEvenOnceExpired()
    {
        var generator = $"llm-{Unique.String(8)}";
        foreach (var (ms, tokens) in new[]
                 {
                     (1000, 100), (3000, 300)
                 })
        {
            var entry = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32)));
            await using var scope = PostgresDatabase.CreateScope();
            await scope.Repository().CompleteAsync(
                new Entry
                {
                    Id = entry.Id,
                    ResponseStatus = 200,
                    ResponseBody = "ok",
                    Model = "honeyslop",
                    Generator = generator,
                    PromptTokens = 10,
                    CompletionTokens = tokens,
                    GenerationMs = ms
                },
                "test",
                DateTimeOffset.UtcNow,
                CancellationToken.None);
            if (ms == 1000)
                await scope.Repository().ExpireAsync([entry.Id], DateTimeOffset.UtcNow, CancellationToken.None);
        }

        await using var readScope = PostgresDatabase.CreateScope();
        var statistics = (await readScope.Repository().GetGeneratorActivityAsync(DateTimeOffset.UtcNow.AddHours(-1), CancellationToken.None))
            .Single(x => x.Generator == generator);
        var totals = await readScope.Repository().GetTotalsAsync(CancellationToken.None);

        Assert.AreEqual(2, statistics.Generated);
        Assert.AreEqual(2000, statistics.AverageMs, 0.1);
        Assert.AreEqual(200, statistics.AverageTokens, 0.1);
        Assert.IsNotNull(statistics.LastGeneratedAt);
        Assert.IsGreaterThanOrEqualTo(0, totals.Queued);
    }

    [TestMethod]
    public async Task GetPageAsync_FiltersAndSorts()
    {
        var host = Unique.Host();
        var entry = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), host: host, target: "/phpinfo.php"));
        await using var scope = PostgresDatabase.CreateScope();

        var page = await scope.Repository().GetPageAsync(host, null, null, EntryOrder.Hits, SortDirection.Desc, 0, 10, CancellationToken.None);
        var totals = await scope.Repository().GetTotalsAsync(CancellationToken.None);

        Assert.AreEqual(1, page.Length);
        Assert.AreEqual(entry.Id, page[0].Id);
        Assert.AreEqual("/phpinfo.php", page[0].Target);
        Assert.IsNull(page[0].ResponseBody);
        Assert.IsGreaterThanOrEqualTo(1, totals.Generating);
    }

    [TestMethod]
    public async Task Writes_CutRequestAndResponseContentToTheColumnLimits()
    {
        var pending = PendingEntry(RandomNumberGenerator.GetBytes(32));
        pending.RequestHeaders = new string('h', Entry.RequestHeadersMaxLength + 10);
        pending.RequestBody = new string('b', Entry.RequestBodyMaxLength + 10);
        var entry = await AddAsync(pending);
        await using (var scope = PostgresDatabase.CreateScope())
        {
            await scope.Repository().CompleteAsync(
                new Entry
                {
                    Id = entry.Id,
                    ResponseStatus = 200,
                    ResponseHeaders =
                    [
                        .. Enumerable.Range(0, Entry.ResponseHeadersMaxCount + 5).Select(_ => new ResponseHeader
                        {
                            Name = new string('n', ResponseHeader.NameMaxLength + 10),
                            Value = new string('v', ResponseHeader.ValueMaxLength + 10)
                        })
                    ],
                    ResponseBody = new string('r', Entry.ResponseBodyMaxLength + 10)
                },
                "test",
                DateTimeOffset.UtcNow,
                CancellationToken.None);
        }

        var stored = await GetAsync(entry.Id);

        Assert.AreEqual(Entry.RequestHeadersMaxLength, stored.RequestHeaders?.Length);
        Assert.AreEqual(Entry.RequestBodyMaxLength, stored.RequestBody?.Length);
        Assert.AreEqual(Entry.ResponseBodyMaxLength, stored.ResponseBody?.Length);
        Assert.HasCount(Entry.ResponseHeadersMaxCount, stored.ResponseHeaders);
        Assert.IsTrue(stored.ResponseHeaders.All(x => x.Name.Length == ResponseHeader.NameMaxLength && x.Value.Length == ResponseHeader.ValueMaxLength));
    }

    [TestMethod]
    public async Task ReplaceAsync_ExpiresTheActiveEntryOfTheKeyAndAddsTheReplacement()
    {
        var keyHash = RandomNumberGenerator.GetBytes(32);
        var current = await AddAsync(PendingEntry(keyHash, EntryStatus.Ready));
        var replacement = PendingEntry(keyHash);
        replacement.Guidance = "Answer as a Hikvision camera";
        replacement.ChangedBy = "operator";

        await using (var scope = PostgresDatabase.CreateScope())
        {
            Assert.IsTrue(await scope.Repository().TryReplaceAsync([replacement], DateTimeOffset.UtcNow, CancellationToken.None));
        }

        var expired = await GetAsync(current.Id);
        var added = await GetAsync(replacement.Id);
        Assert.AreEqual(EntryStatus.Expired, expired.Status);
        Assert.AreEqual(EntryStatus.Pending, added.Status);
        Assert.AreEqual("Answer as a Hikvision camera", added.Guidance);
        Assert.AreEqual("operator", added.ChangedBy);
        CollectionAssert.AreEqual(keyHash, added.KeyHash);
    }

    [TestMethod]
    public async Task TryReplaceAsync_WhenTheKeyGetsAnotherActiveEntry_ChangesNothing()
    {
        var keyHash = RandomNumberGenerator.GetBytes(32);
        var current = await AddAsync(PendingEntry(keyHash, EntryStatus.Ready));

        await using (var scope = PostgresDatabase.CreateScope())
        {
            Assert.IsFalse(await scope.Repository().TryReplaceAsync([PendingEntry(keyHash), PendingEntry(keyHash)], DateTimeOffset.UtcNow, CancellationToken.None));
        }

        Assert.AreEqual(EntryStatus.Ready, (await GetAsync(current.Id)).Status);
    }

    [TestMethod]
    public async Task SetResponseDelayAsync_ChangesOnlyEntriesThatAreServed()
    {
        var ready = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), EntryStatus.Ready));
        var expired = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), EntryStatus.Expired));

        long[] changed;
        await using (var scope = PostgresDatabase.CreateScope())
        {
            changed = await scope.Repository().SetResponseDelayAsync([ready.Id, expired.Id], 1200, DateTimeOffset.UtcNow, CancellationToken.None);
        }

        CollectionAssert.AreEqual(new[]
        {
            ready.Id
        }, changed);
        Assert.AreEqual(1200, (await GetAsync(ready.Id)).ResponseDelayMilliseconds);
        Assert.IsNull((await GetAsync(expired.Id)).ResponseDelayMilliseconds);
    }

    [TestMethod]
    public async Task SetNoiseAsync_ChangesOnlyEntriesThatAreServed()
    {
        var ready = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), EntryStatus.Ready));
        var pending = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32)));
        var expired = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), EntryStatus.Expired));

        long[] changed;
        await using (var scope = PostgresDatabase.CreateScope())
        {
            changed = await scope.Repository().SetNoiseAsync([ready.Id, pending.Id, expired.Id], NoiseTier.Tier2, DateTimeOffset.UtcNow, CancellationToken.None);
        }

        CollectionAssert.AreEqual(new[]
        {
            ready.Id, pending.Id
        }, changed);
        Assert.AreEqual(NoiseTier.Tier2, (await GetAsync(ready.Id)).Noise);
        Assert.AreEqual(NoiseTier.Tier2, (await GetAsync(pending.Id)).Noise);
        Assert.IsNull((await GetAsync(expired.Id)).Noise);
    }

    [TestMethod]
    public async Task GetChangeableAsync_LeavesOutExpiredEntriesWhosePathHasANewerOne()
    {
        var ready = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), EntryStatus.Ready));
        var alone = await AddAsync(PendingEntry(RandomNumberGenerator.GetBytes(32), EntryStatus.Expired));
        var replacedKey = RandomNumberGenerator.GetBytes(32);
        var replaced = await AddAsync(PendingEntry(replacedKey, EntryStatus.Expired));
        var current = await AddAsync(PendingEntry(replacedKey, EntryStatus.Ready));
        await using var scope = PostgresDatabase.CreateScope();

        var changeable = await scope.Repository().GetChangeableAsync([ready.Id, alone.Id, replaced.Id, current.Id], CancellationToken.None);

        CollectionAssert.AreEqual(new[]
        {
            ready.Id, alone.Id, current.Id
        }, changeable.Select(x => x.Id).ToArray());
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