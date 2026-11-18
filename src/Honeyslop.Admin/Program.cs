using Honeyslop.Admin.Access;
using Honeyslop.Admin.Clients;
using Honeyslop.Admin.Configuration;
using Honeyslop.Admin.Live;
using Honeyslop.Admin.Middleware;
using Honeyslop.Admin.Options;
using Honeyslop.Admin.Services;
using Honeyslop.Common;
using Honeyslop.Data;
using Honeyslop.Llm;
using Honeyslop.Reports;
using Microsoft.AspNetCore.DataProtection;

await Host
    .CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(builder => builder
        .AddGlobalCancellationToken()
        .AddJsonConsoleLogging()
        .AddDatabase(AppConfiguration.DbConnectionString)
        .AddAccess()
        .ConfigureKestrel(options => options.AddServerHeader = false)
        .ConfigureServices(services =>
        {
            services
                .AddSingleton(TimeProvider.System)
                .AddSingleton<SecurityHeadersMiddleware>()
                .AddScoped<ICacheMaintenance, CacheMaintenance>()
                .AddHostedService<MaintenanceService>()
                .AddMemoryCache()
                .AddSingleton<IGenerationOutputStore, GenerationOutputStore>()
                .AddSingleton<ILiveUpdateHub, LiveUpdateHub>()
                .AddSingleton<IHostedService>(serviceProvider => (IHostedService)serviceProvider.GetRequiredService<ILiveUpdateHub>())
                .AddHostedService(serviceProvider => ActivatorUtilities.CreateInstance<PostgresNotificationListener>(
                    serviceProvider,
                    AppConfiguration.DbConnectionString))
                .AddSingleton<ICountryLookup>(serviceProvider => new CountryLookup(
                    AppConfiguration.GeoIpDatabase,
                    serviceProvider.GetRequiredService<ILogger<CountryLookup>>()))
                .AddScoped<IWhoisLookup, WhoisLookup>()
                .AddScoped<IUserAgentProvider, UserAgentProvider>()
                .AddRequestArchives()
                .AddHealthChecks();
            services.AddHttpClient<IRipeStatClient, RipeStatClient>(RipeStatClient.Configure);
            services
                .AddDataProtection()
                .UseEphemeralDataProtectionProvider();
            services
                .AddOptions<UserAgentOptions>()
                .Configure(x => x.Default = AppConfiguration.UserAgent);
            services
                .AddOptions<AbuseReportOptions>()
                .Configure(x => x.DefaultSenderName = SenderName.Clean(AppConfiguration.AbuseReportSenderName));
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
            .UseForwardedHeaders()
            .UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(20)
            })
            .UseStaticFiles()
            .UseRouting()
            .UseAuthentication()
            .UseAuthorization()
            .UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.Map("/live", LiveEndpoint.HandleAsync);
                endpoints.MapHealthChecks("/health").AllowAnonymous();
                if (AppConfiguration.SignInEnabled)
                {
                    endpoints.MapGet("/signin", AccountEndpoints.SignIn).AllowAnonymous();
                    endpoints.MapPost("/signout", AccountEndpoints.SignOutAsync).AllowAnonymous();
                }
            }))
        .UseUrls($"http://+:{AppConfiguration.Port}"))
    .Build()
    .RunAsync();
