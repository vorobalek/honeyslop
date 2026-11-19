using Honeyslop.Data.Entities;
using Honeyslop.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Data.Tests;

[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public sealed class DelayJobRepositoryTests
{
    [TestMethod]
    public async Task TryClaimNextAsync_ConcurrentWorkers_TakeEveryJobOnce()
    {
        var now = DateTimeOffset.UtcNow;
        await using (var scope = PostgresDatabase.CreateScope())
        {
            await Repository(scope).ClearAsync(CancellationToken.None);
            await Repository(scope).AddRangeAsync(
                [.. Enumerable.Range(0, 40).Select(x => Job($"203.0.113.{x % 5}", x % 3 == 0, 1000 + x, now))],
                CancellationToken.None);
        }

        var taken = await Task.WhenAll(Enumerable.Range(0, 4).Select(worker => Task.Run(async () =>
        {
            var mine = new List<long>();
            while (true)
            {
                await using var scope = PostgresDatabase.CreateScope();
                var job = await Repository(scope).TryClaimNextAsync($"worker-{worker}", now, now.AddMinutes(-1), CancellationToken.None);
                if (job is null)
                    return mine;
                mine.Add(job.Id);
                Assert.IsTrue(await Repository(scope).CompleteAsync(job.Id, $"worker-{worker}", CancellationToken.None));
            }
        })));

        long[] all = [.. taken.SelectMany(x => x)];
        Assert.HasCount(40, all);
        Assert.HasCount(40, all.Distinct().ToArray());
    }

    [TestMethod]
    public async Task TryClaimNextAsync_StaleClaim_IsTakenOver_AndTheOldWorkerCannotCompleteIt()
    {
        var now = DateTimeOffset.UtcNow;
        await using var scope = PostgresDatabase.CreateScope();
        var repository = Repository(scope);
        await repository.ClearAsync(CancellationToken.None);
        await repository.AddRangeAsync([Job("203.0.113.9", true, 5000, now)], CancellationToken.None);

        var first = await repository.TryClaimNextAsync("dead", now.AddMinutes(-5), now.AddMinutes(-6), CancellationToken.None);
        var alive = await repository.TryClaimNextAsync("alive", now, now.AddMinutes(-1), CancellationToken.None);
        var nothing = await repository.TryClaimNextAsync("other", now, now.AddMinutes(-1), CancellationToken.None);

        Assert.AreEqual(first!.Id, alive!.Id);
        Assert.IsNull(nothing);
        Assert.IsFalse(await repository.CompleteAsync(first.Id, "dead", CancellationToken.None));
        Assert.IsTrue(await repository.CompleteAsync(alive.Id, "alive", CancellationToken.None));
    }

    private static DelayJob Job(string ip, bool answered, int durationMs, DateTimeOffset now)
    {
        return new DelayJob
        {
            ClientIp = ip,
            Answered = answered,
            DurationMs = durationMs,
            CreatedAt = now
        };
    }

    private static IDelayJobRepository Repository(AsyncServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<IDelayJobRepository>();
    }
}