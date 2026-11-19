using Honeyslop.Migrator.Internals;

namespace Honeyslop.Migrator.Extensions;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddMigrator()
        {
            return services.AddScoped<IMigrator, DatabaseMigrator>();
        }
    }
}