using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Enums;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Admin.Mcp.Tools;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using ModelContextProtocol;
using Moq;
using Settings = Honeyslop.Admin.Mcp.Tools.Settings;

namespace Honeyslop.Admin.Tests.Mcp;

[TestClass]
public sealed class ToolsTests
{
    [TestMethod]
    public async Task ExpireMatchingAsync_WithoutAFilter_NeedsEverythingSpelledOut()
    {
        var entries = new Mock<IEntriesService>();
        entries.Setup(x => x.ExpireMatchingAsync(It.IsAny<EntryFilter>(), It.IsAny<CancellationToken>())).ReturnsAsync(7);
        var tools = new Entries(entries.Object);
        var everything = new EntryFilter();

        await Assert.ThrowsExactlyAsync<McpException>(() => tools.ExpireMatchingAsync());
        var expired = await tools.ExpireMatchingAsync(everything: true);

        Assert.AreEqual(7, expired);
        entries.Verify(x => x.ExpireMatchingAsync(everything, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ReplaceAsync_PassesTheHeadersAsNamedPairs()
    {
        var entries = new Mock<IEntriesService>();
        EntryResponseDto? asked = null;
        entries
            .Setup(x => x.ReplaceAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<EntryResponseDto>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<long>, EntryResponseDto, CancellationToken>((_, response, _) => asked = response)
            .ReturnsAsync(new EntryChangeDto(new Dictionary<string, string>(), [9]));

        var change = await new Entries(entries.Object).ReplaceAsync(
            [3],
            200,
            "<?xml version=\"1.0\"?>",
            new Dictionary<string, string>
            {
                ["Content-Type"] = "application/xml",
                ["Server"] = "App-webs/"
            });

        CollectionAssert.AreEqual(new long[]
        {
            9
        }, change.Ids);
        Assert.IsNotNull(asked);
        Assert.AreEqual(200, asked.Status);
        CollectionAssert.AreEqual(
            new[]
            {
                "Content-Type: application/xml", "Server: App-webs/"
            },
            asked.Headers.Select(x => $"{x.Name}: {x.Value}").ToArray());
    }

    [TestMethod]
    public async Task ListAsync_PassesEveryFilterAndTheSortThrough()
    {
        var requests = new Mock<IRequestsService>();
        RequestsQuery? asked = null;
        requests
            .Setup(x => x.GetPageAsync(It.IsAny<RequestsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<RequestsQuery, CancellationToken>((query, _) => asked = query)
            .ReturnsAsync(new RequestsPageDto(new PagedDto<RequestRecord>([], 0, 0, 100), new Dictionary<string, CountryDto>(), DateTimeOffset.UnixEpoch));
        var since = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

        await new Requests(requests.Object).ListAsync(
            "192.0.2.1",
            method: "POST",
            userAgent: "curl",
            outcome: RequestOutcome.Hit,
            since: since,
            sort: RequestColumn.Duration,
            descending: true,
            page: 2);

        Assert.AreEqual(
            new RequestsQuery(
                new RequestFilter("192.0.2.1", Outcome: RequestOutcome.Hit, Method: "POST", UserAgent: "curl", Since: since),
                new Sorting<RequestColumn>(RequestColumn.Duration, SortDirection.Desc),
                2),
            asked);
    }

    [TestMethod]
    public async Task UpdateAsync_ChangesOnlyTheGivenSettings()
    {
        var settings = new Mock<ISettingsService>();
        var current = HoneyslopSettings.Default with
        {
            UserAgent = "custom"
        };
        settings
            .Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SettingsDto(current, "default", null, null));
        HoneyslopSettings? saved = null;
        settings
            .Setup(x => x.SaveAsync(It.IsAny<HoneyslopSettings>(), It.IsAny<CancellationToken>()))
            .Callback<HoneyslopSettings, CancellationToken>((value, _) => saved = value)
            .ReturnsAsync(new SettingsSaveDto(new Dictionary<string, string>(), new MaintenanceResultDto(0, 0, 0, 0)));

        await new Settings(settings.Object).UpdateAsync(3600, publicView: true);

        Assert.AreEqual(current with
        {
            CacheTtl = TimeSpan.FromHours(1),
            PublicView = true
        }, saved);
    }
}