using Honeyslop.Admin.Core.Clients;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Options;
using Honeyslop.Admin.Core.Services.Internals;
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

    private static ReportsService Service(ViewerDto viewer, AbuseReport report)
    {
        var repository = new Mock<IAbuseReportRepository>();
        repository.Setup(x => x.GetByIdOrDefaultAsync(report.Id, It.IsAny<CancellationToken>())).ReturnsAsync(report);
        return new ReportsService(
            repository.Object,
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