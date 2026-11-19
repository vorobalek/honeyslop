using Honeyslop.Common;
using Honeyslop.Data;
using Honeyslop.Migrator;
using Honeyslop.Migrator.Configuration;
using Honeyslop.Migrator.Extensions;

var host = Host
    .CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(builder => builder
        .AddGlobalCancellationToken()
        .AddJsonConsoleLogging()
        .AddDatabase(AppConfiguration.DbConnectionString)
        .ConfigureServices(services => services
            .AddMigrator()))
    .Build();

await using var asyncScope = host
    .Services
    .CreateAsyncScope();

await asyncScope
    .ServiceProvider
    .GetRequiredService<IMigrator>()
    .MigrateAsync();