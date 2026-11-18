using Honeyslop.Admin.Clients;
using Honeyslop.Data.Repositories;

namespace Honeyslop.Admin.Tests.Clients;

[TestClass]
public sealed class UserAgentProviderTests
{
    [TestMethod]
    public async Task GetAsync_ConcurrentCalls_ReadTheSettingsOnce()
    {
        var settings = new SlowSettings(HoneyslopSettings.Default with { UserAgent = "Honeyslop-test (+https://example.test)" });
        var provider = new UserAgentProvider(settings, Microsoft.Extensions.Options.Options.Create(new UserAgentOptions { Default = "default" }));

        var userAgents = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => provider.GetAsync(CancellationToken.None)));

        Assert.AreEqual(1, settings.Reads);
        Assert.IsTrue(userAgents.All(x => x == "Honeyslop-test (+https://example.test)"));
    }

    [TestMethod]
    public async Task GetAsync_NoSetting_UsesTheEnvironmentDefault()
    {
        var provider = new UserAgentProvider(new SlowSettings(HoneyslopSettings.Default), Microsoft.Extensions.Options.Options.Create(new UserAgentOptions { Default = "default" }));

        Assert.AreEqual("default", await provider.GetAsync(CancellationToken.None));
    }

    private sealed class SlowSettings(HoneyslopSettings settings) : ISettingsRepository
    {
        private int _active;

        public int Reads { get; private set; }

        public async Task<HoneyslopSettings> GetAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _active) > 1)
                throw new InvalidOperationException("A second operation was started on this context");
            Reads++;
            await Task.Delay(20, cancellationToken);
            Interlocked.Decrement(ref _active);
            return settings;
        }

        public Task SaveAsync(HoneyslopSettings settings, DateTimeOffset now, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
