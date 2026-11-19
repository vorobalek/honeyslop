using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Honeyslop.Data.Tests;

[TestClass]
[TestCategory("Integration")]
public sealed class MigrationTests
{
    [TestMethod]
    public async Task Migrate_CreatesTheDatabaseOwnedByTheServiceRole()
    {
        await using var connection = new NpgsqlConnection(PostgresDatabase.SuperuserConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "select pg_get_userbyid(datdba) from pg_database where datname = @database",
            connection);
        command.Parameters.AddWithValue("database", PostgresDatabase.Database);

        var owner = await command.ExecuteScalarAsync();

        Assert.AreEqual(PostgresDatabase.Role, owner);
    }

    [TestMethod]
    public async Task Migrate_SecondRun_HasNothingToApply()
    {
        await using var scope = PostgresDatabase.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database;

        await database.MigrateAsync();

        Assert.AreEqual(0, (await database.GetPendingMigrationsAsync()).Count());
    }
}