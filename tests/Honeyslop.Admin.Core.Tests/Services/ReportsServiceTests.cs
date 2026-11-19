using Honeyslop.Admin.Core.Clients;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Options;
using Honeyslop.Admin.Core.Services.Internals;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;
using Moq;

namespace Honeyslop.Admin.Core.Tests.Services;

[TestClass]
public sealed class ReportsServiceTests
{
    [TestMethod]
    public async Task GetAsync_Anonymous_LeavesOutTheSenderAndTheRequester()
    {
        var details = await Service(ViewerDto.Anonymous, Queued()).GetAsync(7, CancellationToken.None);

        Assert.IsNotNull(details);
        Assert.IsNull(details.From);
        Assert.IsFalse(details.FromIsExpected);
        Assert.IsNull(details.Report.RequestedBy);
        Assert.AreEqual(AbuseReportState.Queued, details.Report.State);
    }

    [TestMethod]
    public async Task GetAsync_Operator_SeesTheExpectedSenderUntilTheReportIsSent()
    {
        var details = await Service(Viewers.Operator, Queued()).GetAsync(7, CancellationToken.None);

        Assert.IsNotNull(details);
        Assert.AreEqual("Example Sender <security@example.test>", details.From);
        Assert.IsTrue(details.FromIsExpected);
        Assert.AreEqual("operator", details.Report.RequestedBy);
    }

    [TestMethod]
    public async Task GetAsync_Operator_SeesTheSenderTheReportWentOutWith()
    {
        var report = Queued();
        report.Status = AbuseReportStatus.Sent;
        report.SentFrom = "Old Name <security@example.test>";

        var details = await Service(Viewers.Operator, report).GetAsync(7, CancellationToken.None);

        Assert.AreEqual("Old Name <security@example.test>", details!.From);
        Assert.IsFalse(details.FromIsExpected);
        Assert.AreEqual(AbuseReportState.Sent, details.Report.State);
    }

    [TestMethod]
    public async Task GetAsync_ListsTheBouncesOfTheReport()
    {
        var bounce = new AbuseReportBounce
        {
            ReportId = 7,
            Recipient = "abuse@example.test",
            Action = BounceAction.Failed,
            Status = "5.0.0",
            Diagnostic = "retry timeout exceeded",
            Mta = "mx.example.test",
            NotificationId = "notice@mx.example.test",
            BouncedAt = DateTimeOffset.UnixEpoch
        };

        var details = await Service(ViewerDto.Anonymous, Queued(), [bounce]).GetAsync(7, CancellationToken.None);

        Assert.AreEqual(
            new ReportBounceDto("abuse@example.test", BounceAction.Failed, "5.0.0", "retry timeout exceeded", "mx.example.test", DateTimeOffset.UnixEpoch),
            details!.Bounces.Single());
    }

    [TestMethod]
    public async Task GetBouncedContactsAsync_MarksTheBlockedOnes()
    {
        var bounces = new Mock<IAbuseReportBounceRepository>();
        bounces
            .Setup(x => x.GetContactsPageAsync(DateTimeOffset.UnixEpoch.AddDays(-7), "example", 0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Contact("abuse@dead.example"), Contact("abuse@alive.example")]);
        bounces
            .Setup(x => x.CountContactsAsync(DateTimeOffset.UnixEpoch.AddDays(-7), "example", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        var blocked = new Mock<IBlockedAbuseContactRepository>();
        blocked
            .Setup(x => x.GetAnyOfAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new BlockedAbuseContact { Contact = "@dead.example" }]);

        var page = await Service(Viewers.Operator, Queued(), [], bounces.Object, blocked.Object)
            .GetBouncedContactsAsync(new BouncedContactsQuery(7, "example"), CancellationToken.None);

        Assert.AreEqual(2, page.Contacts.Total);
        CollectionAssert.AreEqual(new[] { true, false }, page.Contacts.Items.Select(x => x.Blocked).ToArray());
    }

    private static BouncedContact Contact(string recipient)
    {
        return new BouncedContact
        {
            Recipient = recipient,
            Bounces = 1,
            Reports = 1,
            LastBouncedAt = DateTimeOffset.UnixEpoch,
            LastReportId = 7,
            LastStatus = "5.0.0"
        };
    }

    private static ReportsService Service(
        ViewerDto viewer,
        AbuseReport report,
        AbuseReportBounce[]? reportBounces = null,
        IAbuseReportBounceRepository? bounceRepository = null,
        IBlockedAbuseContactRepository? blockedRepository = null)
    {
        var repository = new Mock<IAbuseReportRepository>();
        repository.Setup(x => x.GetByIdOrDefaultAsync(report.Id, It.IsAny<CancellationToken>())).ReturnsAsync(report);
        var bounces = new Mock<IAbuseReportBounceRepository>();
        bounces.Setup(x => x.GetByReportAsync(report.Id, It.IsAny<CancellationToken>())).ReturnsAsync(reportBounces ?? []);
        return new ReportsService(
            repository.Object,
            bounceRepository ?? bounces.Object,
            blockedRepository ?? Mock.Of<IBlockedAbuseContactRepository>(),
            Mock.Of<ICountryLookup>(),
            Microsoft.Extensions.Options.Options.Create(new AbuseReportOptions
            {
                FromAddress = "security@example.test"
            }),
            Viewers.Guard(viewer),
            Viewers.ServerTime(DateTimeOffset.UnixEpoch));
    }

    private static AbuseReport Queued()
    {
        return new AbuseReport
        {
            Id = 7,
            ClientIp = "192.0.2.1",
            Recipients = ["abuse@example.test"],
            Subject = "HTTP scanning from 192.0.2.1",
            Body = "Hello",
            RequestedBy = "operator",
            SenderName = "Example Sender",
            Status = AbuseReportStatus.Pending
        };
    }
}