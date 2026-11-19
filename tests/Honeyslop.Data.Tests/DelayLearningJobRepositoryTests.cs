using Honeyslop.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Data.Tests;

[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public sealed class DelayLearningJobRepositoryTests
{
    [TestMethod]
    public async Task EnqueueAsync_KeepsOneWaitingJobPerClient_AndAnotherBesideTheOneInWork()
    {
        var now = DateTimeOffset.UtcNow;
        await using var scope = PostgresDatabase.CreateScope();
        var repository = Repository(scope);
        await repository.ClearAsync(CancellationToken.None);

        await repository.EnqueueAsync(["203.0.113.1", "203.0.113.2"], now, CancellationToken.None);
        await repository.EnqueueAsync(["203.0.113.1"], now, CancellationToken.None);
        var waitingBefore = await repository.CountWaitingAsync(CancellationToken.None);
        var claimed = await repository.TryClaimNextAsync("worker", now, now.AddMinutes(-1), CancellationToken.None);
        await repository.EnqueueAsync(["203.0.113.1", "203.0.113.2"], now, CancellationToken.None);
        await repository.EnqueueAsync(["203.0.113.1"], now, CancellationToken.None);

        Assert.AreEqual(2, waitingBefore);
        Assert.AreEqual("203.0.113.1", claimed!.ClientIp);
        Assert.AreEqual(2, await repository.CountWaitingAsync(CancellationToken.None));
        Assert.AreEqual(3, await repository.ClearAsync(CancellationToken.None));
    }

    [TestMethod]
    public async Task TryClaimNextAsync_ClientInWork_WaitsUntilItsJobIsDone()
    {
        var now = DateTimeOffset.UtcNow;
        await using var scope = PostgresDatabase.CreateScope();
        var repository = Repository(scope);
        await repository.ClearAsync(CancellationToken.None);
        await repository.EnqueueAsync(["203.0.113.1"], now, CancellationToken.None);

        var first = await repository.TryClaimNextAsync("one", now, now.AddMinutes(-1), CancellationToken.None);
        await repository.EnqueueAsync(["203.0.113.1", "203.0.113.2"], now, CancellationToken.None);
        var other = await repository.TryClaimNextAsync("two", now, now.AddMinutes(-1), CancellationToken.None);
        var nothing = await repository.TryClaimNextAsync("three", now, now.AddMinutes(-1), CancellationToken.None);
        Assert.IsTrue(await repository.CompleteAsync(first!.Id, "one", CancellationToken.None));
        var again = await repository.TryClaimNextAsync("three", now, now.AddMinutes(-1), CancellationToken.None);

        Assert.AreEqual("203.0.113.2", other!.ClientIp);
        Assert.IsNull(nothing);
        Assert.AreEqual("203.0.113.1", again!.ClientIp);
        Assert.AreNotEqual(first.Id, again.Id);
    }

    [TestMethod]
    public async Task TryClaimNextAsync_StaleClaim_IsTakenOver_AndTheOldWorkerCannotCompleteIt()
    {
        var now = DateTimeOffset.UtcNow;
        await using var scope = PostgresDatabase.CreateScope();
        var repository = Repository(scope);
        await repository.ClearAsync(CancellationToken.None);
        await repository.EnqueueAsync(["203.0.113.9"], now, CancellationToken.None);

        var first = await repository.TryClaimNextAsync("dead", now.AddMinutes(-5), now.AddMinutes(-6), CancellationToken.None);
        await repository.EnqueueAsync(["203.0.113.9"], now, CancellationToken.None);
        var alive = await repository.TryClaimNextAsync("alive", now, now.AddMinutes(-1), CancellationToken.None);
        var nothing = await repository.TryClaimNextAsync("other", now, now.AddMinutes(-1), CancellationToken.None);

        Assert.AreEqual(first!.Id, alive!.Id);
        Assert.IsNull(nothing);
        Assert.IsFalse(await repository.CompleteAsync(first.Id, "dead", CancellationToken.None));
        Assert.IsTrue(await repository.CompleteAsync(alive.Id, "alive", CancellationToken.None));
    }

    [TestMethod]
    public async Task TryClaimNextAsync_ConcurrentWorkers_NeverHoldOneClientTwice()
    {
        var now = DateTimeOffset.UtcNow;
        await using (var scope = PostgresDatabase.CreateScope())
        {
            await Repository(scope).ClearAsync(CancellationToken.None);
            await Repository(scope).EnqueueAsync([.. Enumerable.Range(0, 30).Select(x => $"203.0.113.{x}")], now, CancellationToken.None);
        }

        var held = new HashSet<string>();
        var overlaps = 0;
        var taken = await Task.WhenAll(Enumerable.Range(0, 4).Select(worker => Task.Run(async () =>
        {
            var mine = new List<string>();
            while (true)
            {
                await using var scope = PostgresDatabase.CreateScope();
                var job = await Repository(scope).TryClaimNextAsync($"worker-{worker}", now, now.AddMinutes(-1), CancellationToken.None);
                if (job is null)
                    return mine;
                lock (held)
                    if (!held.Add(job.ClientIp))
                        overlaps++;
                mine.Add(job.ClientIp);
                await Repository(scope).EnqueueAsync([job.ClientIp], now, CancellationToken.None);
                lock (held)
                    held.Remove(job.ClientIp);
                Assert.IsTrue(await Repository(scope).CompleteAsync(job.Id, $"worker-{worker}", CancellationToken.None));
                if (mine.Count > 40)
                    return mine;
            }
        })));

        Assert.AreEqual(0, overlaps);
        Assert.IsTrue(taken.Sum(x => x.Count) >= 30);
        await using var check = PostgresDatabase.CreateScope();
        await Repository(check).ClearAsync(CancellationToken.None);
    }

    private static IDelayLearningJobRepository Repository(AsyncServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<IDelayLearningJobRepository>();
    }
}