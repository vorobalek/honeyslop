using Honeyslop.Data.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Honeyslop.Data.Tests;

[TestClass]
public static class PostgresDatabase
{
    public const string Role = "honeyslop";
    public const string Database = "honeyslop";
    private const string RolePassword = "honeyslop";

    private static PostgreSqlContainer? _container;
    private static IHost? _host;

    public static string SuperuserConnectionString => _container!.GetConnectionString();

    public static string RoleConnectionString => new NpgsqlConnectionStringBuilder(SuperuserConnectionString)
    {
        Database = Database,
        Username = Role,
        Password = RolePassword
    }.ConnectionString;

    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        _container = new PostgreSqlBuilder("postgres:18").Build();
        await _container.StartAsync();

        await using (var connection = new NpgsqlConnection(SuperuserConnectionString))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"create role {Role} with login createdb password '{RolePassword}'", connection);
            await command.ExecuteNonQueryAsync();
        }

        _host = new HostBuilder()
            .ConfigureWebHost(builder => builder
                .AddDatabase(RoleConnectionString)
                .Configure(_ => { }))
            .Build();
        await using var scope = _host.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        _host?.Dispose();
        if (_container is not null)
            await _container.DisposeAsync();
    }

    public static AsyncServiceScope CreateScope()
    {
        return _host!.Services.CreateAsyncScope();
    }

    public static IEntryRepository Repository(this AsyncServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<IEntryRepository>();
    }
}