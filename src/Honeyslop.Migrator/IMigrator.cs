namespace Honeyslop.Migrator;

internal interface IMigrator
{
    Task MigrateAsync();
}