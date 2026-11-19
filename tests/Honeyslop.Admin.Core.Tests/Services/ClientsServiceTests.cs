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
        public Mock<IAbuseReportRepository> Repository { get; } = new();
        public AbuseReport? Added { get; private set; }

        public ClientsService Service(ViewerDto? viewer = null)
        {
            var requests = new Mock<IRequestRepository>();
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
            return new ClientsService(
                requests.Object,
                Mock.Of<IEntryRepository>(),
                Repository.Object,
                settings.Object,
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