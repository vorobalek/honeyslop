using Honeyslop.Data.Repositories;
using Honeyslop.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Data.Tests;

[TestClass]
[TestCategory("Integration")]
public sealed class ClientWhoisRepositoryTests
{
    [TestMethod]
    public async Task SaveAsync_InsertsThenReplacesTheStoredAnswer()
    {
        var clientIp = $"198.51.100.{Random.Shared.Next(1, 254)}-{Unique.String(6)}";
        var now = DateTimeOffset.UtcNow;

        await using (var scope = PostgresDatabase.CreateScope())
            await scope.ServiceProvider.GetRequiredService<IClientWhoisRepository>()
                .SaveAsync(clientIp, """{"Prefix": "first"}""", now.AddDays(-1), CancellationToken.None);
        await using (var scope = PostgresDatabase.CreateScope())
            await scope.ServiceProvider.GetRequiredService<IClientWhoisRepository>()
                .SaveAsync(clientIp, """{"Prefix": "second"}""", now, CancellationToken.None);

        await using var readScope = PostgresDatabase.CreateScope();
        var stored = await readScope.ServiceProvider.GetRequiredService<IClientWhoisRepository>()
            .GetOrDefaultAsync(clientIp, CancellationToken.None);

        Assert.IsNotNull(stored);
        Assert.Contains("second", stored.Info);
        Assert.AreEqual(now.ToUnixTimeMilliseconds(), stored.UpdatedAt.ToUnixTimeMilliseconds());
    }
}
