using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Honeyslop.Admin.Core.Enums;
using Honeyslop.Admin.Live;
using Honeyslop.Admin.Live.Contracts.Watches;
using Honeyslop.Admin.Live.Enums;
using Honeyslop.Admin.Views;
using Honeyslop.Admin.Views.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Honeyslop.Admin.Tests.Live;

[TestClass]
public sealed class WatchHandlersTests
{
    private static readonly JsonSerializerOptions HubOptions = new(JsonSerializerDefaults.Web)
    {
        AllowOutOfOrderMetadataProperties = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ServiceProvider _services = new ServiceCollection().AddLiveWatches().BuildServiceProvider();

    [TestMethod]
    public void EveryView_HasAHandler()
    {
        var views = typeof(WatchRequest).GetCustomAttributes<JsonDerivedTypeAttribute>().Select(x => x.DerivedType).ToArray();

        var handled = _services.GetServices<IWatchHandler>().Select(x => x.RequestType).ToArray();

        CollectionAssert.AreEquivalent(views, handled);
    }

    [TestMethod]
    [DataRow("{\"view\":\"entry\",\"id\":5}")]
    [DataRow("{\"id\":5,\"view\":\"entry\"}")]
    public void Request_ArrivesAsItsOwnType(string json)
    {
        Assert.AreEqual(new EntryWatch(5), JsonSerializer.Deserialize<WatchRequest>(json, HubOptions));
    }

    [TestMethod]
    public void Query_ArrivesWithItsFilterAndSorting()
    {
        const string json = "{\"view\":\"entries\",\"query\":{\"filter\":{\"host\":\"cam\",\"target\":null,\"state\":\"Ready\"},\"sort\":{\"column\":\"Hits\",\"direction\":\"Desc\"},\"page\":2}}";

        var request = JsonSerializer.Deserialize<WatchRequest>(json, HubOptions) as EntriesWatch;

        Assert.IsNotNull(request);
        Assert.AreEqual("cam", request.Query.Filter.Host);
        Assert.AreEqual(EntryColumn.Hits, request.Query.Sort?.Column);
        Assert.AreEqual(2, request.Query.Page);
    }

    [TestMethod]
    public async Task Handler_BuildsTheViewFromTheTypedRequest()
    {
        var views = new Mock<IViewBuilder>();
        var view = new EntryViewDto(null!, default, false, null, null, null, DateTimeOffset.UnixEpoch);
        views.Setup(x => x.GetEntryAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(view);
        var handler = _services.GetRequiredService<IWatchHandlers>().For(new EntryWatch(5));

        var built = await handler.BuildAsync(views.Object, new EntryWatch(5), CancellationToken.None);

        Assert.AreSame(view, built);
        Assert.AreEqual(WatchAudience.Shared, handler.Audience);
        Assert.AreEqual(5, handler.InterestIn(new EntryWatch(5)).EntryId);
    }

    [TestMethod]
    public void PersonalViews_AreTheOnesWithTheViewersOwnSettings()
    {
        var personal = _services
            .GetServices<IWatchHandler>()
            .Where(x => x.Audience == WatchAudience.Personal)
            .Select(x => x.RequestType)
            .ToArray();

        CollectionAssert.AreEquivalent(new[] { typeof(ClientAbuseWatch), typeof(SettingsWatch) }, personal);
    }
}
