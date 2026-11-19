using System.Text.Json.Serialization;
using Honeyslop.Admin.Access;
using Honeyslop.Admin.Access.Internals;
using Honeyslop.Admin.Configuration;
using Honeyslop.Admin.Core;
using Honeyslop.Admin.Core.Access;
using Honeyslop.Admin.Core.Options;
using Honeyslop.Admin.Core.Text;
using Honeyslop.Admin.Hubs;
using Honeyslop.Admin.Live;
using Honeyslop.Admin.Live.Internals;
using Honeyslop.Admin.Mcp;
using Honeyslop.Admin.Middleware;
using Honeyslop.Admin.Views;
using Honeyslop.Admin.Views.Internals;
using Honeyslop.Common;
using Honeyslop.Data;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Requests;
using Honeyslop.Llm;
using Microsoft.AspNetCore.SignalR;

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
                .AddScoped<ViewerContext>()
                .AddScoped<IViewerContext>(serviceProvider => serviceProvider.GetRequiredService<ViewerContext>())
                .AddScoped<IViewerScope>(serviceProvider => serviceProvider.GetRequiredService<ViewerContext>())
                .AddScoped<IViewBuilder, ViewBuilder>()
                .AddSingleton<ILiveSender, HubLiveSender>()
                .AddSingleton<LiveSubscriptions>()
                .AddSingleton<ILiveSubscriptions>(serviceProvider => serviceProvider.GetRequiredService<LiveSubscriptions>())
                .AddHostedService(serviceProvider => serviceProvider.GetRequiredService<LiveSubscriptions>())
                .AddLiveWatches()
                .AddHostedService(serviceProvider => ActivatorUtilities.CreateInstance<PostgresNotificationListener>(
                    serviceProvider,
                    AppConfiguration.DbConnectionString))
                .AddHealthChecks();
            services
                .AddOptions<CountryOptions>()
                .Configure(x => x.DatabasePath = AppConfiguration.GeoIpDatabase);
            services
                .AddOptions<UserAgentOptions>()
                .Configure(x => x.Default = AppConfiguration.UserAgent);
            services
                .AddOptions<ResponseDelayOptions>()
                .Configure(x => x.DefaultMilliseconds = Math.Clamp(AppConfiguration.ResponseDelayMilliseconds, 0, Entry.ResponseDelayMaxMilliseconds));
            services
                .AddOptions<AbuseReportOptions>()
                .Configure(x =>
                {
                    x.DefaultSenderName = SenderName.Clean(AppConfiguration.AbuseReportSenderName);
                    x.FromAddress = AppConfiguration.SmtpFrom;
                });
            services
                .AddOptions<RequestRules>()
                .Configure(x => x.Apply(AppConfiguration.MaxTargetLength, AppConfiguration.StaticExtensions));
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
            services
                .AddSignalR(options => options.AddFilter<AdminHubFilter>())
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.PayloadSerializerOptions.AllowOutOfOrderMetadataProperties = true;
                });
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
                endpoints
                    .MapHub<AdminHub>("/hub")
                    .AllowAnonymous();
                endpoints.MapGet("/clients/{ip}/archive", ContentEndpoints.ArchiveAsync);
                endpoints.MapGet("/entries/{id:long}/preview", ContentEndpoints.PreviewAsync);
                endpoints.MapHoneyslopMcp();
                endpoints.MapHealthChecks("/health").AllowAnonymous();
                if (AppConfiguration.SignInEnabled)
                {
                    endpoints.MapGet("/signin", AccountEndpoints.SignIn).AllowAnonymous();
                    endpoints.MapPost("/signout", (Delegate)AccountEndpoints.SignOutAsync).AllowAnonymous();
                }

                endpoints.MapFallbackToFile("index.html");
                endpoints.MapFallbackToFile("/clients/{ip}", "index.html");
            }))
        .UseUrls($"http://+:{AppConfiguration.Port}"))
    .Build()
    .RunAsync();