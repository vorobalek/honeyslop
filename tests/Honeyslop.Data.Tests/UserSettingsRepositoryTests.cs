using Honeyslop.Data.Repositories;
using Honeyslop.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Data.Tests;

[TestClass]
[TestCategory("Integration")]
public sealed class UserSettingsRepositoryTests
{
    [TestMethod]
    public async Task SaveAsync_KeepsSettingsPerUserAndClearsEmptyOnes()
    {
        var user = $"user-{Unique.String(8)}";
        var other = $"user-{Unique.String(8)}";
        var now = DateTimeOffset.UtcNow;

        await SaveAsync(user, new UserSettings("Example Researcher, security team"), now);
        var saved = await GetAsync(user);
        var otherUser = await GetAsync(other);
        await SaveAsync(user, UserSettings.Default, now);
        var cleared = await GetAsync(user);

        Assert.AreEqual("Example Researcher, security team", saved.SenderName);
        Assert.IsNull(otherUser.SenderName);
        Assert.IsNull(cleared.SenderName);
    }

    private static async Task SaveAsync(string userId, UserSettings settings, DateTimeOffset now)
    {
        await using var scope = PostgresDatabase.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IUserSettingsRepository>().SaveAsync(userId, settings, now, CancellationToken.None);
    }

    private static async Task<UserSettings> GetAsync(string userId)
    {
        await using var scope = PostgresDatabase.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IUserSettingsRepository>().GetAsync(userId, CancellationToken.None);
    }
}
