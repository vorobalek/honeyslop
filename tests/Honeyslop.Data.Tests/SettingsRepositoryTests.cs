using Honeyslop.Data.Repositories;
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
        Assert.AreEqual(HoneyslopSettings.Default, await repository.GetAsync(CancellationToken.None));

        var saved = new HoneyslopSettings(
            TimeSpan.FromHours(6),
            TimeSpan.FromDays(14),
            false,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(90),
            false,
            TimeSpan.FromMinutes(3),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(4));
        await repository.SaveAsync(saved, DateTimeOffset.UtcNow, CancellationToken.None);
        await repository.SaveAsync(saved with { GenerationEnabled = true }, DateTimeOffset.UtcNow, CancellationToken.None);

        await using var readScope = PostgresDatabase.CreateScope();
        var loaded = await readScope.ServiceProvider.GetRequiredService<ISettingsRepository>().GetAsync(CancellationToken.None);
        Assert.AreEqual(saved with { GenerationEnabled = true }, loaded);
    }
}
