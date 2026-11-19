using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Trap.Generation.Internals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Honeyslop.Trap.Tests.Generation;

[TestClass]
public sealed class EntryWatcherTests
{
    [TestMethod]
    public async Task PollAsync_EntryBecameReady_WakesEveryWaiter()
    {
        var repository = new Mock<IEntryRepository>();
        repository
            .Setup(x => x.GetFinishedAsync(It.Is<IReadOnlyCollection<long>>(ids => ids.Contains(3)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Entry
                {
                    Id = 3,
                    Status = EntryStatus.Ready,
                    ResponseStatus = 200,
                    ResponseBody = "ok"
                }
            ]);
        using var watcher = Create(repository.Object);

        var first = watcher.WaitAsync(3, TimeSpan.FromSeconds(5), CancellationToken.None);
        var second = watcher.WaitAsync(3, TimeSpan.FromSeconds(5), CancellationToken.None);
        await watcher.PollAsync(CancellationToken.None);

        Assert.AreEqual(200, (await first)?.Status);
        Assert.AreEqual(200, (await second)?.Status);
    }

    [TestMethod]
    public async Task PollAsync_EntryOutOfAttempts_ReleasesWaitersWithoutResponse()
    {
        var repository = new Mock<IEntryRepository>();
        repository
            .Setup(x => x.GetFinishedAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Entry
                {
                    Id = 4,
                    Status = EntryStatus.Failed,
                    Attempts = 3
                }
            ]);
        using var watcher = Create(repository.Object);

        var waiting = watcher.WaitAsync(4, TimeSpan.FromSeconds(30), CancellationToken.None);
        await watcher.PollAsync(CancellationToken.None);

        Assert.IsNull(await waiting.WaitAsync(TimeSpan.FromSeconds(1)));
    }

    [TestMethod]
    public async Task PollAsync_EntryFailedWithAttemptsLeft_KeepsWaiting()
    {
        var repository = new Mock<IEntryRepository>();
        repository
            .Setup(x => x.GetFinishedAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Entry
                {
                    Id = 4,
                    Status = EntryStatus.Failed,
                    Attempts = 1
                }
            ]);
        using var watcher = Create(repository.Object);

        var waiting = watcher.WaitAsync(4, TimeSpan.FromSeconds(30), CancellationToken.None);
        await watcher.PollAsync(CancellationToken.None);

        Assert.IsFalse(waiting.IsCompleted);
        watcher.Complete(4, null);
        Assert.IsNull(await waiting);
    }

    private static EntryWatcher Create(IEntryRepository repository)
    {
        var services = new ServiceCollection()
            .AddSingleton(repository)
            .BuildServiceProvider();
        return new EntryWatcher(
            services.GetRequiredService<IServiceScopeFactory>(),
            TestOptions.Trap(),
            TimeProvider.System,
            NullLogger<EntryWatcher>.Instance);
    }
}