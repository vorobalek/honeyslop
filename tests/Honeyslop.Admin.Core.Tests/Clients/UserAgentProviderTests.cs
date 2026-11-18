using Honeyslop.Admin.Core.Clients.Internals;
using Honeyslop.Admin.Core.Options;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Repositories;

namespace Honeyslop.Admin.Core.Tests.Clients;

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

        public async Task<Settings> GetAsync(string? userId, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _active) > 1)
                throw new InvalidOperationException("A second operation was started on this context");
            Reads++;
            await Task.Delay(20, cancellationToken);
            Interlocked.Decrement(ref _active);
            return new Settings { Honeyslop = settings };
        }

        public Task SaveAsync(HoneyslopSettings saved, DateTimeOffset now, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task SaveAsync(UserSettings saved, DateTimeOffset now, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
