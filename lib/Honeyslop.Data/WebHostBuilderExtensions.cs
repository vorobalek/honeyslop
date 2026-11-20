using Honeyslop.Data.Providers;
using Honeyslop.Data.Providers.Internals;
using Honeyslop.Data.Repositories;
using Honeyslop.Data.Repositories.Internals;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Honeyslop.Data;

public static class WebHostBuilderExtensions
{
    extension(IWebHostBuilder builder)
    {
        public IWebHostBuilder AddDatabase(string? connectionString = null)
        {
            return builder.ConfigureServices((context, services) =>
            {
                services
                    .AddScoped<IEntryRepository, EntryRepository>()
                    .AddScoped<ISettingsRepository, SettingsRepository>()
                    .AddScoped<IRequestRepository, RequestRepository>()
                    .AddScoped<IClientWhoisRepository, ClientWhoisRepository>()
                    .AddScoped<IAbuseReportRepository, AbuseReportRepository>()
                    .AddScoped<IBlockedAbuseContactRepository, BlockedAbuseContactRepository>()
                    .AddScoped<IAbuseReportBounceRepository, AbuseReportBounceRepository>()
                    .AddScoped<IMailboxCursorRepository, MailboxCursorRepository>()
                    .AddScoped<IClientDelayRepository, ClientDelayRepository>()
                    .AddScoped<IClientProbeRepository, ClientProbeRepository>()
                    .AddScoped<IDelayLearningJobRepository, DelayLearningJobRepository>()
                    .AddScoped<IServerTimeRepository, ServerTimeRepository>()
                    .AddScoped<IDataProtectionKeyRepository, DataProtectionKeyRepository>()
                    .AddDbContext<ApplicationDbContext>(optionsBuilder => optionsBuilder
                        .UseNpgsql(
                            connectionString ?? context.Configuration.GetConnectionString("Default"),
                            options => options.MigrationsAssembly("Honeyslop.Migrator")));
            });
        }

        public IWebHostBuilder AddSettingsCache()
        {
            return builder.ConfigureServices(services => services.AddSingleton<ISettingsProvider, SettingsProvider>());
        }
    }
}