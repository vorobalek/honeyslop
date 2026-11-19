using System.Security.Cryptography;
using Honeyslop.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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

    [TestMethod]
    public async Task Migrate_DelaysInSeconds_BecomeMilliseconds()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(PostgresDatabase.RoleConnectionString)
        {
            Database = $"conversion_{Unique.String(8).ToLowerInvariant()}"
        }.ConnectionString;
        await using var services = new ServiceCollection()
            .AddDbContext<ApplicationDbContext>(x => x.UseNpgsql(connectionString, y => y.MigrationsAssembly("Honeyslop.Migrator")))
            .BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();
        var migrator = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().GetService<IMigrator>();
        await migrator.MigrateAsync("20261119171021_ef_abuse_report_bounces");

        long entryId;
        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await using (var command = new NpgsqlCommand(
                             "insert into entry (key_hash, method, host, target, status, attempts, hits, created_at, updated_at, response_delay_seconds) " +
                             "values (@key, 'GET', 'a.example', '/', 1, 0, 0, now(), now(), 3) returning id",
                             connection))
            {
                command.Parameters.AddWithValue("key", RandomNumberGenerator.GetBytes(32));
                entryId = (long)(await command.ExecuteScalarAsync())!;
            }

            await using (var command = new NpgsqlCommand("insert into setting (key, value, updated_at) values ('response_delay_seconds', '7', now())", connection))
                await command.ExecuteNonQueryAsync();
        }

        await migrator.MigrateAsync("20261119180806_ef_millisecond_and_client_delays");

        await using var check = new NpgsqlConnection(connectionString);
        await check.OpenAsync();
        await using var delay = new NpgsqlCommand("select response_delay_milliseconds from entry where id = @id", check);
        delay.Parameters.AddWithValue("id", entryId);
        await using var setting = new NpgsqlCommand("select value from setting where key = 'response_delay_milliseconds'", check);
        Assert.AreEqual(3000, await delay.ExecuteScalarAsync());
        Assert.AreEqual("7000", await setting.ExecuteScalarAsync());

        await migrator.MigrateAsync();

        await using var left = new NpgsqlCommand(
            "select (select count(*) from information_schema.columns where table_name = 'entry' and column_name = 'response_delay_seconds') + " +
            "(select count(*) from setting where key = 'response_delay_seconds')",
            check);
        Assert.AreEqual(0L, await left.ExecuteScalarAsync());
        Assert.AreEqual(3000, await delay.ExecuteScalarAsync());
    }
}