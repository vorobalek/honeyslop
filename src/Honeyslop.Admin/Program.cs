using Honeyslop.Admin.Configuration;
using Honeyslop.Admin.Live;
using Honeyslop.Admin.Middleware;
using Honeyslop.Admin.Options;
using Honeyslop.Admin.Services;
using Honeyslop.Common;
using Honeyslop.Data;
using Honeyslop.Llm;
using Microsoft.AspNetCore.DataProtection;

await Host
    .CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(builder => builder
        .AddGlobalCancellationToken()
        .AddJsonConsoleLogging()
        .AddDatabase(AppConfiguration.DbConnectionString)
        .ConfigureKestrel(options => options.AddServerHeader = false)
        .ConfigureServices(services =>
        {
            services
                .AddSingleton(TimeProvider.System)
                .AddSingleton<SecurityHeadersMiddleware>()
                .AddScoped<ICacheMaintenance, CacheMaintenance>()
                .AddHostedService<MaintenanceService>()
                .AddMemoryCache()
                .AddSingleton<GenerationOutputStore>()
                .AddSingleton<LiveUpdateHub>()
                .AddHostedService(serviceProvider => serviceProvider.GetRequiredService<LiveUpdateHub>())
                .AddHostedService(serviceProvider => ActivatorUtilities.CreateInstance<PostgresNotificationListener>(
                    serviceProvider,
                    AppConfiguration.DbConnectionString))
                .AddHealthChecks();
            services
                .AddDataProtection()
                .UseEphemeralDataProtectionProvider();
            services
                .AddOptions<MaintenanceOptions>()
                .Configure(x => x.Interval = TimeSpan.FromSeconds(AppConfiguration.MaintenanceIntervalSeconds));
            services.AddLlm(x =>
            {
                x.Urls =
                [
                    ..(AppConfiguration.LlmUrl ?? string.Empty)
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(url => new Uri(url.TrimEnd('/') + "/"))
                ];
                x.Timeout = TimeSpan.FromSeconds(10);
                x.ProbeTimeout = TimeSpan.FromSeconds(2);
                x.DiscoveryInterval = TimeSpan.FromSeconds(15);
            });
            services.AddRazorPages();
        })
        .Configure(app => app
            .UseMiddleware<SecurityHeadersMiddleware>()
            .UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(20)
            })
            .UseStaticFiles()
            .UseRouting()
            .UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.Map("/live", LiveEndpoint.HandleAsync);
            })
            .UseHealthChecks("/health"))
        .UseUrls($"http://+:{AppConfiguration.Port}"))
    .Build()
    .RunAsync();
