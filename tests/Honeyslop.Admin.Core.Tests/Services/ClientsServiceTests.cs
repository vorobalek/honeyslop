using Honeyslop.Admin.Core.Clients;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Enums;
using Honeyslop.Admin.Core.Exceptions;
using Honeyslop.Admin.Core.Options;
using Honeyslop.Admin.Core.Services.Internals;
using Honeyslop.Admin.Core.Text;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Honeyslop.Reports;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Honeyslop.Admin.Core.Tests.Services;

[TestClass]
public sealed class ClientsServiceTests
{
    private const string Ip = "192.0.2.1";
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task ReportAsync_Anonymous_IsDenied()
    {
        var setup = new Setup();

        await Assert.ThrowsExactlyAsync<AccessDeniedException>(() =>
            setup.Service(ViewerDto.Anonymous).ReportAsync(Ip, null, CancellationToken.None));
    }

    [TestMethod]
    public async Task ReportAsync_UnknownAddress_IsNotFound()
    {
        var setup = new Setup
        {
            Summary = null
        };

        var request = await setup.Service().ReportAsync(Ip, null, CancellationToken.None);

        Assert.AreEqual(ReportOutcome.NotFound, request.Outcome);
    }

    [TestMethod]
    public async Task ReportAsync_ReportOnItsWay_PointsAtIt()
    {
        var setup = new Setup
        {
            Reports = [Report(5, AbuseReportStatus.Pending)]
        };

        var request = await setup.Service().ReportAsync(Ip, null, CancellationToken.None);

        Assert.AreEqual(new ReportRequestDto(ReportOutcome.AlreadyQueued, 5), request);
        setup.Repository.Verify(x => x.TryAddAsync(It.IsAny<AbuseReport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ReportAsync_NothingNewSinceTheLastSentReport_IsAlreadyReported()
    {
        var setup = new Setup
        {
            Reports = [Report(5, AbuseReportStatus.Sent, 40)],
            NewSinceLastSent = 0
        };

        var request = await setup.Service().ReportAsync(Ip, null, CancellationToken.None);

        Assert.AreEqual(ReportOutcome.AlreadyReported, request.Outcome);
    }

    [TestMethod]
    public async Task ReportAsync_NoAbuseContact_HasNobodyToWriteTo()
    {
        var setup = new Setup
        {
            Contacts = []
        };

        var request = await setup.Service().ReportAsync(Ip, null, CancellationToken.None);

        Assert.AreEqual(ReportOutcome.NoContact, request.Outcome);
    }

    [TestMethod]
    public async Task ReportAsync_EveryContactBlocked_QueuesNothing()
    {
        var setup = new Setup
        {
            Contacts = ["Abuse@Bouncing.example", "noc@bouncing.example"],
            Blocked = ["abuse@bouncing.example", "@bouncing.example"]
        };

        var request = await setup.Service().ReportAsync(Ip, null, CancellationToken.None);

        Assert.AreEqual(ReportOutcome.Blocked, request.Outcome);
        Assert.IsNull(setup.Added);
    }

    [TestMethod]
    public async Task ReportAsync_SomeContactsBlocked_WritesOnlyToTheOthers()
    {
        var setup = new Setup
        {
            Contacts = ["abuse@bouncing.example", "abuse@example.test"],
            Blocked = ["abuse@bouncing.example"]
        };

        var request = await setup.Service().ReportAsync(Ip, null, CancellationToken.None);

        Assert.AreEqual(ReportOutcome.Queued, request.Outcome);
        CollectionAssert.AreEqual(new[]
        {
            "abuse@example.test"
        }, setup.Added!.Recipients);
    }

    [TestMethod]
    public async Task GetAbuseAsync_ShowsBlockedContactsApartFromTheRecipients()
    {
        var setup = new Setup
        {
            Contacts = ["abuse@bouncing.example", "abuse@example.test"],
            Blocked = ["@bouncing.example"]
        };

        var abuse = await setup.Service().GetAbuseAsync(Ip, CancellationToken.None);

        CollectionAssert.AreEqual(new[]
        {
            "abuse@bouncing.example"
        }, abuse!.BlockedRecipients);
        CollectionAssert.AreEqual(new[]
        {
            "abuse@example.test"
        }, abuse.Recipients);
    }

    [TestMethod]
    public async Task GetAbuseAsync_ShowsHowTheContactsBounced()
    {
        var setup = new Setup
        {
            Contacts = ["Abuse@Bouncing.example", "abuse@example.test"],
            Blocked = ["@bouncing.example"],
            Bounced =
            [
                new BouncedContact
                {
                    Recipient = "abuse@bouncing.example",
                    Bounces = 3,
                    Reports = 2,
                    LastBouncedAt = Now,
                    LastReportId = 22,
                    LastStatus = "5.0.0",
                    LastDiagnostic = "retry timeout exceeded"
                }
            ]
        };

        var abuse = await setup.Service().GetAbuseAsync(Ip, CancellationToken.None);

        var bounced = abuse!.Bounces.Single();
        Assert.AreEqual(new BouncedContactDto("abuse@bouncing.example", 3, 2, Now, 22, "5.0.0", "retry timeout exceeded", true), bounced);
        CollectionAssert.AreEquivalent(new[]
        {
            "abuse@bouncing.example", "abuse@example.test"
        }, setup.BounceLookups.Single());
    }

    [TestMethod]
    public async Task SetResponseDelayAsync_SavesTheDelayOfTheNormalizedAddress()
    {
        var setup = new Setup();

        var v6 = await setup.Service().SetResponseDelayAsync("2001:DB8::0:7", 1500, CancellationToken.None);
        var mapped = await setup.Service().SetResponseDelayAsync("::ffff:198.51.100.4", null, CancellationToken.None);

        Assert.IsTrue(v6.Changed && mapped.Changed);
        setup.Delays.Verify(x => x.SetAsync("2001:db8::7", 1500, "operator", Now, It.IsAny<CancellationToken>()), Times.Once);
        setup.Delays.Verify(x => x.SetAsync("198.51.100.4", null, "operator", Now, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    [DataRow("not-an-ip", 100, "Ip")]
    [DataRow(Ip, -1, "ResponseDelayMilliseconds")]
    [DataRow(Ip, 600_001, "ResponseDelayMilliseconds")]
    public async Task SetResponseDelayAsync_WrongAddressOrDelay_IsRefused(string ip, int milliseconds, string field)
    {
        var setup = new Setup();

        var change = await setup.Service().SetResponseDelayAsync(ip, milliseconds, CancellationToken.None);

        CollectionAssert.AreEqual(new[]
        {
            field
        }, change.Errors.Keys.ToArray());
        setup.Delays.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ResetResponseDelaysAsync_RemovesEveryClientDelay()
    {
        var setup = new Setup();
        setup.Delays.Setup(x => x.ClearAsync(It.IsAny<CancellationToken>())).ReturnsAsync(7);

        Assert.AreEqual(7, (await setup.Service().ResetResponseDelaysAsync(CancellationToken.None)).Changed);
    }

    [TestMethod]
    public async Task RelearnDelaysAsync_ForgetsTheLearnedDelaysAndQueuesEveryClientInTheLog()
    {
        var setup = new Setup();
        setup.Probes.Setup(x => x.ClearAsync(It.IsAny<CancellationToken>())).ReturnsAsync(412);
        setup.Requests.Setup(x => x.GetClientIpsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(["192.0.2.7", "::ffff:192.0.2.7", "2001:db8::1", ""]);
        string[]? queued = null;
        setup.Jobs
            .Setup(x => x.EnqueueAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<string>, DateTimeOffset, CancellationToken>((clients, _, _) => queued = [.. clients]);

        var change = await setup.Service().RelearnDelaysAsync(CancellationToken.None);

        Assert.AreEqual(412, change.Changed);
        setup.Jobs.Verify(x => x.ClearAsync(It.IsAny<CancellationToken>()), Times.Once);
        CollectionAssert.AreEqual(new[]
        {
            "192.0.2.7", "2001:db8::1"
        }, queued);
    }

    [TestMethod]
    public async Task ResetDelaysAsync_Anonymous_AreDenied()
    {
        var setup = new Setup();

        await Assert.ThrowsExactlyAsync<AccessDeniedException>(() => setup.Service(ViewerDto.Anonymous).RelearnDelaysAsync(CancellationToken.None));
        await Assert.ThrowsExactlyAsync<AccessDeniedException>(() => setup.Service(ViewerDto.Anonymous).ResetResponseDelaysAsync(CancellationToken.None));
    }

    [TestMethod]
    public async Task SetResponseDelayAsync_Anonymous_IsDenied()
    {
        await Assert.ThrowsExactlyAsync<AccessDeniedException>(() =>
            new Setup().Service(ViewerDto.Anonymous).SetResponseDelayAsync(Ip, 100, CancellationToken.None));
    }

    [TestMethod]
    public async Task ReportAsync_NewRequestsSinceTheLastReport_QueuesAFollowUpSignedWithTheGivenName()
    {
        var setup = new Setup
        {
            Reports = [Report(5, AbuseReportStatus.Sent, 40)],
            NewSinceLastSent = 3
        };

        var request = await setup.Service().ReportAsync(Ip, "  Given\tName ", CancellationToken.None);

        Assert.AreEqual(ReportOutcome.Queued, request.Outcome);
        Assert.AreEqual("Given Name", setup.Added!.SenderName);
        Assert.AreEqual("operator", setup.Added.RequestedBy);
        CollectionAssert.AreEqual(new[]
        {
            "abuse@example.test"
        }, setup.Added.Recipients);
        Assert.AreEqual(Now, setup.Added.CreatedAt);
    }

    [TestMethod]
    public async Task ReportAsync_WithoutAName_SignsWithThePersonalNameBeforeTheDefault()
    {
        var setup = new Setup
        {
            PersonalSenderName = "Personal Name"
        };

        await setup.Service().ReportAsync(Ip, " ", CancellationToken.None);

        Assert.AreEqual("Personal Name", setup.Added!.SenderName);
    }

    [TestMethod]
    public async Task GetAbuseAsync_Anonymous_GetsNoDraftAndNoRequesters()
    {
        var setup = new Setup
        {
            Reports = [Report(5, AbuseReportStatus.Sent, 40)],
            NewSinceLastSent = 2
        };

        var abuse = await setup.Service(ViewerDto.Anonymous).GetAbuseAsync(Ip, CancellationToken.None);

        Assert.IsNotNull(abuse);
        Assert.IsNull(abuse.Draft);
        Assert.IsNull(abuse.Reports.Single().RequestedBy);
        Assert.AreEqual(2, abuse.NewSinceLastSent);
        Assert.IsFalse(abuse.AlreadyReported);
    }

    private static AbuseReport Report(long id, AbuseReportStatus status, long? lastRequestId = null)
    {
        return new AbuseReport
        {
            Id = id,
            ClientIp = Ip,
            Recipients = ["abuse@example.test"],
            Subject = "HTTP scanning from 192.0.2.1",
            Body = "Hello",
            RequestedBy = "operator",
            Status = status,
            LastRequestId = lastRequestId
        };
    }

    private sealed class Setup
    {
        public ClientActivity? Summary { get; init; } = new()
        {
            ClientIp = Ip,
            Requests = 4,
            Hosts = 1,
            Targets = 2,
            FirstSeenAt = Now.AddDays(-1),
            LastSeenAt = Now.AddHours(-1),
            LastUserAgent = "curl/8.4.0"
        };

        public AbuseReport[] Reports { get; init; } = [];
        public int NewSinceLastSent { get; init; }
        public string[] Contacts { get; init; } = ["abuse@example.test"];
        public string? PersonalSenderName { get; init; }
        public string[] Blocked { get; init; } = [];
        public BouncedContact[] Bounced { get; init; } = [];
        public List<string[]> BounceLookups { get; } = [];
        public Mock<IClientDelayRepository> Delays { get; } = new();
        public Mock<IClientProbeRepository> Probes { get; } = new();
        public Mock<IDelayLearningJobRepository> Jobs { get; } = new();
        public Mock<IRequestRepository> Requests { get; } = new();
        public Mock<IAbuseReportRepository> Repository { get; } = new();
        public AbuseReport? Added { get; private set; }

        public ClientsService Service(ViewerDto? viewer = null)
        {
            var requests = Requests;
            requests.Setup(x => x.GetClientOrDefaultAsync(Ip, It.IsAny<CancellationToken>())).ReturnsAsync(Summary);
            requests.Setup(x => x.CountAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<RequestOutcome?>(), It.IsAny<long?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(), It.Is<long?>(id => id != null), It.IsAny<CancellationToken>())).ReturnsAsync(NewSinceLastSent);
            requests
                .Setup(x => x.GetPageAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<RequestOutcome?>(), It.IsAny<long?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTimeOffset?>(),
                    It.IsAny<DateTimeOffset?>(), It.IsAny<RequestOrder>(), It.IsAny<SortDirection>(), 0, AbuseLetters.LatestRequests, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            Repository.Setup(x => x.GetByClientAsync(Ip, It.IsAny<CancellationToken>())).ReturnsAsync(Reports);
            Repository
                .Setup(x => x.TryAddAsync(It.IsAny<AbuseReport>(), It.IsAny<CancellationToken>()))
                .Callback<AbuseReport, CancellationToken>((report, _) => Added = report)
                .ReturnsAsync(true);
            var whois = new Mock<IWhoisLookup>();
            whois
                .Setup(x => x.LookupAsync(Ip, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WhoisDto("192.0.2.0/24", [], null, null, null, null, null, Contacts, string.Empty, Now));
            var settings = new Mock<ISettingsRepository>();
            settings
                .Setup(x => x.GetAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string? userId, CancellationToken _) => new Settings
                {
                    User = userId is null
                        ? null
                        : new UserSettings
                        {
                            UserId = userId,
                            SenderName = PersonalSenderName
                        }
                });
            var blocked = new Mock<IBlockedAbuseContactRepository>();
            blocked
                .Setup(x => x.GetAnyOfAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyCollection<string> candidates, CancellationToken _) =>
                [
                    .. Blocked.Where(candidates.Contains).Select(x => new BlockedAbuseContact
                    {
                        Contact = x
                    })
                ]);
            var bounces = new Mock<IAbuseReportBounceRepository>();
            bounces
                .Setup(x => x.GetContactsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyCollection<string>, CancellationToken>((recipients, _) => BounceLookups.Add([.. recipients]))
                .ReturnsAsync(() => Bounced);
            return new ClientsService(
                requests.Object,
                Mock.Of<IEntryRepository>(),
                Repository.Object,
                settings.Object,
                blocked.Object,
                bounces.Object,
                Delays.Object,
                Probes.Object,
                Jobs.Object,
                Mock.Of<IRequestArchiveBuilder>(),
                Mock.Of<ICountryLookup>(),
                whois.Object,
                Microsoft.Extensions.Options.Options.Create(new AbuseReportOptions
                {
                    DefaultSenderName = "Default Name",
                    FromAddress = "security@example.test"
                }),
                Viewers.Guard(viewer ?? Viewers.Operator),
                Viewers.ServerTime(Now),
                NullLogger<ClientsService>.Instance);
        }
    }
}