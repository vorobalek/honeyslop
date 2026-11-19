using Honeyslop.Admin.Core.Access;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Exceptions;
using Honeyslop.Admin.Core.Maintenance;
using Honeyslop.Admin.Core.Options;
using Honeyslop.Admin.Core.Services.Internals;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Repositories;
using Moq;

namespace Honeyslop.Admin.Core.Tests.Services;

[TestClass]
public sealed class SettingsServiceTests
{
    private readonly Mock<ICacheMaintenance> _maintenance = new();
    private readonly Mock<IPublicView> _publicView = new();
    private readonly Mock<ISettingsRepository> _settings = new();

    [TestMethod]
    public async Task SaveAsync_TooShortClaimTimeout_IsRejectedAndNothingIsSaved()
    {
        var save = await Service(Viewers.Operator).SaveAsync(
            HoneyslopSettings.Default with
            {
                ClaimTimeout = TimeSpan.FromSeconds(5),
                UserAgent = "bad\nagent"
            },
            CancellationToken.None);

        Assert.IsFalse(save.Saved);
        CollectionAssert.AreEquivalent(
            new[]
            {
                nameof(HoneyslopSettings.ClaimTimeout), nameof(HoneyslopSettings.UserAgent)
            },
            save.Errors.Keys.ToArray());
        _settings.Verify(x => x.SaveAsync(It.IsAny<HoneyslopSettings>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task SaveAsync_ValidSettings_AreSavedAppliedAndTheBlankUserAgentDropped()
    {
        _maintenance
            .Setup(x => x.RunAsync(It.IsAny<HoneyslopSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MaintenanceResultDto(1, 2, 3, 4));

        var save = await Service(Viewers.Operator).SaveAsync(HoneyslopSettings.Default with
        {
            UserAgent = "  "
        }, CancellationToken.None);

        Assert.IsTrue(save.Saved);
        Assert.AreEqual(new MaintenanceResultDto(1, 2, 3, 4), save.Maintenance);
        _settings.Verify(x => x.SaveAsync(HoneyslopSettings.Default, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()));
        _publicView.Verify(x => x.Reset());
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(600_001)]
    public async Task SaveAsync_ResponseDelayOutOfRange_IsRejected(int milliseconds)
    {
        var result = await Service(Viewers.Operator).SaveAsync(
            HoneyslopSettings.Default with
            {
                ResponseDelayMilliseconds = milliseconds
            },
            CancellationToken.None);

        CollectionAssert.AreEqual(new[]
        {
            nameof(HoneyslopSettings.ResponseDelayMilliseconds)
        }, result.Errors.Keys.ToArray());
        _settings.Verify(x => x.SaveAsync(It.IsAny<HoneyslopSettings>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    [DataRow(-1, 30_000, "AutoDelayMinMilliseconds")]
    [DataRow(30_000, 30_000, "AutoDelayMinMilliseconds")]
    [DataRow(0, 600_001, "AutoDelayMaxMilliseconds")]
    public async Task SaveAsync_WrongAutoDelayRange_IsRejected(int min, int max, string field)
    {
        var result = await Service(Viewers.Operator).SaveAsync(
            HoneyslopSettings.Default with
            {
                AutoDelayEnabled = true,
                AutoDelayMinMilliseconds = min,
                AutoDelayMaxMilliseconds = max
            },
            CancellationToken.None);

        CollectionAssert.AreEqual(new[]
        {
            field
        }, result.Errors.Keys.ToArray());
        _settings.Verify(x => x.SaveAsync(It.IsAny<HoneyslopSettings>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task GetAsync_ShowsTheDefaultDelayFromTheEnvironment()
    {
        _settings.Setup(x => x.GetAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Settings());

        var view = await Service(Viewers.Operator).GetAsync(CancellationToken.None);

        Assert.AreEqual(2000, view.DefaultResponseDelayMilliseconds);
    }

    [TestMethod]
    public async Task GetAsync_Anonymous_DoesNotSeeTheDefaultSenderName()
    {
        _settings.Setup(x => x.GetAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Settings());

        var view = await Service(ViewerDto.Anonymous).GetAsync(CancellationToken.None);

        Assert.IsNull(view.DefaultSenderName);
        Assert.IsNull(view.PersonalSenderName);
    }

    [TestMethod]
    public async Task SavePersonalAsync_Anonymous_IsDenied()
    {
        await Assert.ThrowsExactlyAsync<AccessDeniedException>(() =>
            Service(ViewerDto.Anonymous).SavePersonalAsync("Name", CancellationToken.None));
    }

    private SettingsService Service(ViewerDto viewer)
    {
        return new SettingsService(
            _settings.Object,
            _maintenance.Object,
            _publicView.Object,
            Microsoft.Extensions.Options.Options.Create(new AbuseReportOptions
            {
                DefaultSenderName = "Default Name"
            }),
            Microsoft.Extensions.Options.Options.Create(new UserAgentOptions
            {
                Default = "Honeyslop"
            }),
            Microsoft.Extensions.Options.Options.Create(new ResponseDelayOptions
            {
                DefaultMilliseconds = 2000
            }),
            Viewers.Guard(viewer),
            Viewers.ServerTime(DateTimeOffset.UnixEpoch));
    }
}