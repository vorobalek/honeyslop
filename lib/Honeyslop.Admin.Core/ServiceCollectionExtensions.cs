using Honeyslop.Admin.Core.Access;
using Honeyslop.Admin.Core.Access.Internals;
using Honeyslop.Admin.Core.Clients;
using Honeyslop.Admin.Core.Clients.Internals;
using Honeyslop.Admin.Core.Maintenance;
using Honeyslop.Admin.Core.Maintenance.Internals;
using Honeyslop.Admin.Core.Options;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Admin.Core.Services.Internals;
using Honeyslop.Reports;
using Microsoft.Extensions.Options;

namespace Honeyslop.Admin.Core;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAdminCore()
        {
            services.AddHttpClient<IRipeStatClient, RipeStatClient>(RipeStatClient.Configure);
            return services
                .AddMemoryCache()
                .AddRequestArchives()
                .AddSingleton<IPublicView, PublicView>()
                .AddSingleton<IGenerationOutputStore, GenerationOutputStore>()
                .AddSingleton<ICountryLookup>(serviceProvider => new CountryLookup(
                    serviceProvider.GetRequiredService<IOptions<CountryOptions>>().Value.DatabasePath,
                    serviceProvider.GetRequiredService<ILogger<CountryLookup>>()))
                .AddScoped<IAccessGuard, AccessGuard>()
                .AddScoped<IWhoisLookup, WhoisLookup>()
                .AddScoped<IUserAgentProvider, UserAgentProvider>()
                .AddScoped<ICacheMaintenance, CacheMaintenance>()
                .AddScoped<IEntriesService, EntriesService>()
                .AddScoped<IRequestsService, RequestsService>()
                .AddScoped<IClientsService, ClientsService>()
                .AddScoped<IReportsService, ReportsService>()
                .AddScoped<IGeneratorsService, GeneratorsService>()
                .AddScoped<ISettingsService, SettingsService>()
                .AddHostedService<MaintenanceService>();
        }
    }
}
