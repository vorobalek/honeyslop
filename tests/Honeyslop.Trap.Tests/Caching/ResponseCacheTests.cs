using Honeyslop.Data.Providers;
using Honeyslop.Data.Requests;
using Honeyslop.Trap.Caching.Internals;
using Honeyslop.Trap.Responses;
using Honeyslop.Trap.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Honeyslop.Trap.Tests.Caching;

[TestClass]
public sealed class ResponseCacheTests
{
    private static readonly EntryKey Key = EntryKey.Create("GET", "camera.example", "/SDK/webLanguage");
    private static readonly EntryKey OtherKey = EntryKey.Create("GET", "camera.example", "/ISAPI/System/deviceInfo");

    [TestMethod]
    public void Evict_DropsTheResponseOfThatEntryOnly()
    {
        using var cache = Enabled();
        cache.Set(Key, Response(1), cache.Version);
        cache.Set(OtherKey, Response(2), cache.Version);

        cache.Evict(1);

        Assert.IsFalse(cache.TryGet(Key, out _));
        Assert.IsTrue(cache.TryGet(OtherKey, out var other));
        Assert.AreEqual(2, other.EntryId);
    }

    [TestMethod]
    public void Set_ReadBeforeAnEviction_IsNotCached()
    {
        using var cache = Enabled();
        var version = cache.Version;

        cache.Evict(1);
        cache.Set(Key, Response(1), version);

        Assert.IsFalse(cache.TryGet(Key, out _));
    }

    [TestMethod]
    public void Evict_AfterAnotherEntryTookTheKey_KeepsTheNewOne()
    {
        using var cache = Enabled();
        cache.Set(Key, Response(1), cache.Version);
        cache.Set(Key, Response(2), cache.Version);

        cache.Evict(1);

        Assert.IsTrue(cache.TryGet(Key, out var response));
        Assert.AreEqual(2, response.EntryId);
    }

    [TestMethod]
    public void Disabled_CachesNothingAndDropsWhatItHad()
    {
        using var cache = Enabled();
        cache.Set(Key, Response(1), cache.Version);

        cache.Disable();
        cache.Set(OtherKey, Response(2), cache.Version);

        Assert.IsFalse(cache.TryGet(Key, out _));
        Assert.IsFalse(cache.TryGet(OtherKey, out _));
    }

    [TestMethod]
    public void EntryChange_EvictsByIdAndClearsOnAnythingElse()
    {
        using var cache = Enabled();
        var listener = new ChangeListener(
            "Host=unused",
            cache,
            Mock.Of<ISettingsProvider>(),
            Mock.Of<IClientDelays>(),
            TimeProvider.System,
            NullLogger<ChangeListener>.Instance);
        cache.Set(Key, Response(1), cache.Version);
        cache.Set(OtherKey, Response(2), cache.Version);

        listener.Dispatch("honeyslop_entries", "1");
        Assert.IsFalse(cache.TryGet(Key, out _));
        Assert.IsTrue(cache.TryGet(OtherKey, out _));

        listener.Dispatch("honeyslop_entries", "");
        Assert.IsFalse(cache.TryGet(OtherKey, out _));
    }

    [TestMethod]
    public void SettingsChange_ReloadsTheSettingsAndKeepsTheCache()
    {
        using var cache = Enabled();
        var settings = new Mock<ISettingsProvider>();
        var listener = new ChangeListener("Host=unused", cache, settings.Object, Mock.Of<IClientDelays>(), TimeProvider.System, NullLogger<ChangeListener>.Instance);
        cache.Set(Key, Response(1), cache.Version);

        listener.Dispatch("honeyslop_settings", "");

        settings.Verify(x => x.Reset(), Times.Once);
        Assert.IsTrue(cache.TryGet(Key, out _));
    }

    [TestMethod]
    public void ClientDelayChange_ReloadsTheClientDelaysAndKeepsTheCache()
    {
        using var cache = Enabled();
        var clientDelays = new Mock<IClientDelays>();
        var listener = new ChangeListener("Host=unused", cache, Mock.Of<ISettingsProvider>(), clientDelays.Object, TimeProvider.System, NullLogger<ChangeListener>.Instance);
        cache.Set(Key, Response(1), cache.Version);

        listener.Dispatch("honeyslop_clients", "");

        clientDelays.Verify(x => x.Reset(), Times.Once);
        Assert.IsTrue(cache.TryGet(Key, out _));
    }

    private static ResponseCache Enabled()
    {
        var cache = new ResponseCache(TestOptions.Trap());
        cache.Enable();
        return cache;
    }

    private static CachedResponse Response(long entryId)
    {
        return new CachedResponse(entryId, 200, [], [1, 2, 3]);
    }
}