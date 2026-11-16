using System.Text.Json;
using Honeyslop.Admin.Live;

namespace Honeyslop.Admin.Tests.Live;

[TestClass]
public sealed class LiveUpdateHubTests
{
    [TestMethod]
    public async Task Publish_ManyChanges_ArriveAsOneMessage()
    {
        using var hub = new LiveUpdateHub(TimeProvider.System);
        using var stop = new CancellationTokenSource();
        await hub.StartAsync(stop.Token);
        using var subscription = hub.Subscribe();

        hub.Publish(LiveUpdateHub.Entries, 1);
        hub.Publish(LiveUpdateHub.Entries, 2);
        hub.Publish(LiveUpdateHub.Requests);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var message = JsonDocument.Parse(await subscription.Reader.ReadAsync(timeout.Token)).RootElement;
        CollectionAssert.AreEquivalent(
            new[] { "entries", "requests" },
            message.GetProperty("topics").EnumerateArray().Select(x => x.GetString()).ToArray());
        CollectionAssert.AreEquivalent(
            new[] { 1L, 2L },
            message.GetProperty("entries").EnumerateArray().Select(x => x.GetInt64()).ToArray());
        Assert.IsFalse(subscription.Reader.TryRead(out _));
        await hub.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task Publish_Outputs_AreKeptApartFromEntryChanges()
    {
        using var hub = new LiveUpdateHub(TimeProvider.System);
        await hub.StartAsync(CancellationToken.None);
        using var subscription = hub.Subscribe();

        hub.Publish(LiveUpdateHub.Outputs, 7);
        hub.Publish(LiveUpdateHub.Entries, 1);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var message = JsonDocument.Parse(await subscription.Reader.ReadAsync(timeout.Token)).RootElement;
        CollectionAssert.AreEqual(
            new[] { 1L },
            message.GetProperty("entries").EnumerateArray().Select(x => x.GetInt64()).ToArray());
        CollectionAssert.AreEqual(
            new[] { 7L },
            message.GetProperty("outputs").EnumerateArray().Select(x => x.GetInt64()).ToArray());
        await hub.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task Subscription_AfterDispose_StopsReceiving()
    {
        using var hub = new LiveUpdateHub(TimeProvider.System);
        await hub.StartAsync(CancellationToken.None);
        var subscription = hub.Subscribe();
        subscription.Dispose();

        hub.Publish(LiveUpdateHub.Entries, 1);
        await Task.Delay(600);

        Assert.AreEqual(0, hub.Subscribers);
        Assert.IsFalse(subscription.Reader.TryRead(out _));
        await hub.StopAsync(CancellationToken.None);
    }
}
