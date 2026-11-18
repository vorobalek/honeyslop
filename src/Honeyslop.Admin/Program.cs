using Honeyslop.Admin.Access;
using Honeyslop.Admin.Access.Internals;
using Honeyslop.Admin.Configuration;
using Honeyslop.Admin.Core;
using Honeyslop.Admin.Core.Access;
using Honeyslop.Admin.Core.Options;
using Honeyslop.Admin.Core.Text;
using Honeyslop.Admin.Live;
using Honeyslop.Admin.Mcp;
using Honeyslop.Admin.Live.Internals;
using Honeyslop.Admin.Middleware;
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
        .AddAccess()
        .AddMcp()
        .ConfigureKestrel(options => options.AddServerHeader = false)
        .ConfigureServices(services =>
        {
            services
                .AddSingleton(TimeProvider.System)
                .AddSingleton<SecurityHeadersMiddleware>()
                .AddAdminCore()
                .AddHttpContextAccessor()
                .AddScoped<IViewerContext, HttpViewerContext>()
                .AddSingleton<ILiveUpdateHub, LiveUpdateHub>()
                .AddSingleton<IHostedService>(serviceProvider => (IHostedService)serviceProvider.GetRequiredService<ILiveUpdateHub>())
                .AddHostedService(serviceProvider => ActivatorUtilities.CreateInstance<PostgresNotificationListener>(
                    serviceProvider,
                    AppConfiguration.DbConnectionString))
                .AddHealthChecks();
            services
                .AddDataProtection()
                .UseEphemeralDataProtectionProvider();
            services
                .AddOptions<CountryOptions>()
                .Configure(x => x.DatabasePath = AppConfiguration.GeoIpDatabase);
            services
                .AddOptions<UserAgentOptions>()
                .Configure(x => x.Default = AppConfiguration.UserAgent);
            services
                .AddOptions<AbuseReportOptions>()
                .Configure(x =>
                {
                    x.DefaultSenderName = SenderName.Clean(AppConfiguration.AbuseReportSenderName);
                    x.FromAddress = AppConfiguration.SmtpFrom;
                });
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
                endpoints.MapHoneyslopMcp();
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
