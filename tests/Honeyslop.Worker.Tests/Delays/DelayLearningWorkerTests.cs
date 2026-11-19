using Honeyslop.Data.Contracts;
using Honeyslop.Data.Delays;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Providers;
using Honeyslop.Data.Repositories;
using Honeyslop.Worker.Delays;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Honeyslop.Worker.Tests.Delays;

[TestClass]
public sealed class DelayLearningWorkerTests
{
    private const string Ip = "203.0.113.5";
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 0, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task RunOnceAsync_NewClient_LearnsItsHistoryUpToTheFirstUnfinishedRequest()
    {
        var setup = new Setup();
        setup.History(0, Timing(10, RequestOutcome.Abandoned, 6_000), Timing(11, RequestOutcome.Hit, 5_900), Timing(12, RequestOutcome.Waiting, 0, Now.AddMinutes(-1)), Timing(13, RequestOutcome.Hit, 5_900));

        Assert.IsTrue(await setup.Worker().RunOnceAsync(CancellationToken.None));

        var added = setup.Added.Single();
        Assert.AreEqual((6_000, (int?)6_000, 11L), (added.DelayMilliseconds + 100, added.TimeoutMilliseconds, added.LastRequestId));
        setup.Probes.Verify(x => x.TryReplaceAsync(It.IsAny<ClientProbe>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        setup.Jobs.Verify(x => x.CompleteAsync(7, "test", It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task RunOnceAsync_KnownClient_ContinuesFromItsCursor_AndSkipsRequestsLeftUnfinishedLongAgo()
    {
        var setup = new Setup
        {
            Current = new ClientProbe
            {
                ClientIp = Ip,
                DelayMilliseconds = 5_900,
                TimeoutMilliseconds = 6_000,
                ProbeEvery = 20,
                LastRequestId = 11,
                Version = 3
            }
        };
        setup.History(11, Timing(12, RequestOutcome.Waiting, 0, Now.AddHours(-1)), Timing(13, RequestOutcome.Hit, 5_900));

        Assert.IsTrue(await setup.Worker().RunOnceAsync(CancellationToken.None));

        var replaced = setup.Replaced.Single();
        Assert.AreEqual((3, 13L, 1), (replaced.Version, replaced.Probe.LastRequestId, replaced.Probe.AnswersSinceProbe));
    }

    [TestMethod]
    public async Task RunOnceAsync_LongHistory_IsLearnedPageByPage()
    {
        var setup = new Setup();
        setup.History(0, [.. Enumerable.Range(1, 500).Select(x => Timing(x, RequestOutcome.Hit, 1_000))]);
        setup.History(500, Timing(501, RequestOutcome.Abandoned, 8_000));

        Assert.IsTrue(await setup.Worker().RunOnceAsync(CancellationToken.None));

        Assert.AreEqual(500L, setup.Added.Single().LastRequestId);
        var replaced = setup.Replaced.Single();
        Assert.AreEqual((0, 501L, (int?)8_000), (replaced.Version, replaced.Probe.LastRequestId, replaced.Probe.TimeoutMilliseconds));
    }

    [TestMethod]
    public async Task RunOnceAsync_TrickOff_TakesNothing()
    {
        var setup = new Setup
        {
            Enabled = false
        };

        Assert.IsFalse(await setup.Worker().RunOnceAsync(CancellationToken.None));

        setup.Jobs.Verify(x => x.TryClaimNextAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static RequestRecord Timing(long id, RequestOutcome outcome, int durationMs, DateTimeOffset? receivedAt = null)
    {
        return new RequestRecord
        {
            Id = id,
            ClientIp = Ip,
            Outcome = outcome,
            DurationMs = durationMs,
            ReceivedAt = receivedAt ?? Now.AddHours(-2)
        };
    }

    private sealed class Setup
    {
        public bool Enabled { get; init; } = true;
        public ClientProbe? Current { get; init; }
        public Mock<IDelayLearningJobRepository> Jobs { get; } = new();
        public Mock<IClientProbeRepository> Probes { get; } = new();
        public Mock<IRequestRepository> Requests { get; } = new();
        public List<ClientProbe> Added { get; } = [];
        public List<(ClientProbe Probe, int Version)> Replaced { get; } = [];

        public void History(long afterId, params RequestRecord[] timings)
        {
            Requests
                .Setup(x => x.GetTimingsAsync(It.Is<IReadOnlyCollection<string>>(ips => ips.Contains(Ip)), afterId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(timings);
        }

        public DelayLearningWorker Worker()
        {
            Requests
                .Setup(x => x.GetTimingsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.Is<long>(id => id > 500), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            Jobs
                .Setup(x => x.TryClaimNextAsync("test", Now, Now.AddMinutes(-1), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DelayLearningJob
                {
                    Id = 7,
                    ClientIp = Ip,
                    InWork = true
                });
            Probes.Setup(x => x.GetOrDefaultAsync(Ip, It.IsAny<CancellationToken>())).ReturnsAsync(Current);
            Probes
                .Setup(x => x.TryAddAsync(It.IsAny<ClientProbe>(), It.IsAny<CancellationToken>()))
                .Callback<ClientProbe, CancellationToken>((probe, _) => Added.Add(probe))
                .ReturnsAsync(true);
            Probes
                .Setup(x => x.TryReplaceAsync(It.IsAny<ClientProbe>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback<ClientProbe, int, CancellationToken>((probe, version, _) => Replaced.Add((probe, version)))
                .ReturnsAsync(true);
            var serverTime = new Mock<IServerTimeRepository>();
            serverTime.Setup(x => x.GetNowAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Now);
            var settings = new Mock<ISettingsProvider>();
            settings
                .Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HoneyslopSettings
                {
                    AutoDelayEnabled = Enabled,
                    AutoDelayMinMilliseconds = 0,
                    AutoDelayMaxMilliseconds = 30_000
                });
            var services = new ServiceCollection()
                .AddSingleton(Jobs.Object)
                .AddSingleton(Probes.Object)
                .AddSingleton(Requests.Object)
                .AddSingleton(serverTime.Object)
                .BuildServiceProvider();
            return new DelayLearningWorker(
                services.GetRequiredService<IServiceScopeFactory>(),
                settings.Object,
                new OptionsWrapper<WorkerOptions>(new WorkerOptions
                {
                    InstanceName = "test"
                }),
                TimeProvider.System,
                NullLogger<DelayLearningWorker>.Instance);
        }
    }
}