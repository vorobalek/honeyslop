using Honeyslop.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Data.Tests;

[TestClass]
[TestCategory("Integration")]
public sealed class ServerTimeRepositoryTests
{
    [TestMethod]
    public async Task GetNowAsync_ReadsTheDatabaseClock()
    {
        await using var scope = PostgresDatabase.CreateScope();

        var now = await scope.ServiceProvider.GetRequiredService<IServerTimeRepository>().GetNowAsync(CancellationToken.None);

        Assert.IsLessThan(TimeSpan.FromSeconds(30), (now - DateTimeOffset.UtcNow).Duration());
    }
}
