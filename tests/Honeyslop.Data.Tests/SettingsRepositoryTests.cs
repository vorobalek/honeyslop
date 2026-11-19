using Honeyslop.Data.Contracts;
using Honeyslop.Data.Repositories;
using Honeyslop.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Data.Tests;

[TestClass]
[TestCategory("Integration")]
public sealed class SettingsRepositoryTests
{
    [TestMethod]
    public async Task SaveAsync_ThenGetAsync_RoundTrips()
    {
        await using var scope = PostgresDatabase.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
        Assert.AreEqual(new Settings(), await repository.GetAsync(null, CancellationToken.None));

        var saved = new HoneyslopSettings
        {
            CacheTtl = TimeSpan.FromHours(6),
            KeepUnused = TimeSpan.FromDays(14),
            GenerationEnabled = false,
            RequestRetention = TimeSpan.Zero,
            GenerationWait = TimeSpan.FromSeconds(90),
            AllowRefusals = false,
            ClaimTimeout = TimeSpan.FromMinutes(3),
            RetryFailedAfter = TimeSpan.FromMinutes(5),
            GenerationTimeLimit = TimeSpan.FromMinutes(4),
            PublicView = true,
            UserAgent = "Honeyslop-test (+https://example.test)"
        };
        await repository.SaveAsync(saved, DateTimeOffset.UtcNow, CancellationToken.None);
        await repository.SaveAsync(saved with
        {
            GenerationEnabled = true
        }, DateTimeOffset.UtcNow, CancellationToken.None);

        await using var readScope = PostgresDatabase.CreateScope();
        var loaded = await readScope.ServiceProvider.GetRequiredService<ISettingsRepository>().GetAsync(null, CancellationToken.None);
        Assert.AreEqual(saved with
        {
            GenerationEnabled = true
        }, loaded.Honeyslop);
    }

    [TestMethod]
    public async Task SaveAsync_KeepsUserSettingsPerUserAndClearsEmptyOnes()
    {
        var user = $"user-{Unique.String(8)}";
        var other = $"user-{Unique.String(8)}";
        var now = DateTimeOffset.UtcNow;

        await SaveAsync(new UserSettings
        {
            UserId = user,
            SenderName = "Example Researcher, security team"
        }, now);
        var saved = await GetAsync(user);
        var otherUser = await GetAsync(other);
        await SaveAsync(new UserSettings
        {
            UserId = user
        }, now);
        var cleared = await GetAsync(user);

        Assert.AreEqual("Example Researcher, security team", saved.User?.SenderName);
        Assert.IsNull(otherUser.User?.SenderName);
        Assert.IsNull(cleared.User?.SenderName);
    }

    [TestMethod]
    public async Task SaveAsync_FromSeveralPlacesAtOnce_KeepsOneOfTheValues()
    {
        var user = $"user-{Unique.String(8)}";
        var names = Enumerable.Range(0, 6).Select(i => $"Example Researcher {i}").ToArray();
        var go = new TaskCompletionSource();

        var saving = names.Select(name => Task.Run(async () =>
        {
            await go.Task;
            await SaveAsync(new UserSettings
            {
                UserId = user,
                SenderName = name
            }, DateTimeOffset.UtcNow);
        })).ToArray();
        go.SetResult();
        await Task.WhenAll(saving);

        CollectionAssert.Contains(names, (await GetAsync(user)).User?.SenderName);
    }

    private static async Task SaveAsync(UserSettings settings, DateTimeOffset now)
    {
        await using var scope = PostgresDatabase.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISettingsRepository>().SaveAsync(settings, now, CancellationToken.None);
    }

    private static async Task<Settings> GetAsync(string userId)
    {
        await using var scope = PostgresDatabase.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISettingsRepository>().GetAsync(userId, CancellationToken.None);
    }
}