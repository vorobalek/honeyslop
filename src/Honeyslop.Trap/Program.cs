using System.Runtime.CompilerServices;
using Honeyslop.Common;
using Honeyslop.Data;
using Honeyslop.Trap.Caching;
using Honeyslop.Trap.Configuration;
using Honeyslop.Trap.Generation;
using Honeyslop.Trap.Handlers;
using Honeyslop.Trap.Hits;
using Honeyslop.Llm;
using Honeyslop.Trap.Llm;
using Honeyslop.Trap.Options;
using Honeyslop.Trap.Requests;
using Honeyslop.Trap.Responses;
using Honeyslop.Trap.Settings;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Prometheus;

[assembly: InternalsVisibleTo("Honeyslop.Trap.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

Metrics.SuppressDefaultMetrics();
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
                .AddSingleton<IRequestClassifier, RequestClassifier>()
                .AddSingleton<IResponseCache, ResponseCache>()
                .AddSingleton<IResponseSanitizer, ResponseSanitizer>()
                .AddSingleton<IPromptBuilder, PromptBuilder>()
                .AddSingleton<ILlmDispatcher, LlmDispatcher>()
                .AddSingleton<IGenerationSignal, GenerationSignal>()
                .AddSingleton<EntryWatcher>()
                .AddSingleton<IEntryWatcher>(serviceProvider => serviceProvider.GetRequiredService<EntryWatcher>())
                .AddHostedService(serviceProvider => serviceProvider.GetRequiredService<EntryWatcher>())
                .AddSingleton<IHitCounter, HitCounter>()
                .AddSingleton<ISettingsProvider, SettingsProvider>()
                .AddSingleton<IRequestLog, RequestLog>()
                .AddHostedService<RequestLogWriter>()
                .AddScoped<TrapHandler>()
                .AddHostedService<GenerationWorker>()
                .AddHostedService<HitFlushService>()
                .AddHealthChecks();
            services
                .AddOptions<TrapOptions>()
                .Configure(x =>
                {
                    x.InstanceName = AppConfiguration.InstanceName;
                    x.Workers = GenerationConfiguration.Workers;
                    x.MaxWaiters = GenerationConfiguration.MaxWaiters;
                    x.MaxAttempts = GenerationConfiguration.MaxAttempts;
                    x.MaxTargetLength = GenerationConfiguration.MaxTargetLength;
                    x.MaxRequestBodyBytes = GenerationConfiguration.MaxRequestBodyBytes;
                    x.MemoryCacheDuration = TimeSpan.FromSeconds(GenerationConfiguration.MemoryCacheSeconds);
                    x.MemoryCacheSizeLimit = GenerationConfiguration.MemoryCacheMegabytes * 1024L * 1024L;
                    x.HitFlushInterval = TimeSpan.FromSeconds(GenerationConfiguration.HitFlushSeconds);
                    x.StaticExtensions = GenerationConfiguration.StaticExtensions is { } staticExtensions
                        ? StaticExtensions.Parse(staticExtensions)
                        : StaticExtensions.Default;
                });
            services
                .AddLlm(x =>
                {
                    x.Urls =
                    [
                        ..LlmConfiguration.Url
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(url => new Uri(url.TrimEnd('/') + "/"))
                    ];
                    x.Model = LlmConfiguration.Model;
                    x.MaxTokens = LlmConfiguration.MaxTokens;
                    x.Temperature = LlmConfiguration.Temperature;
                    x.Timeout = Timeout.InfiniteTimeSpan;
                    x.DiscoveryInterval = TimeSpan.FromSeconds(LlmConfiguration.DiscoverySeconds);
                    x.SlotPollInterval = TimeSpan.FromMilliseconds(LlmConfiguration.SlotPollMilliseconds);
                    x.ProbeTimeout = TimeSpan.FromSeconds(2);
                    x.UnhealthyCooldown = TimeSpan.FromSeconds(10);
                    x.DispatchGrace = TimeSpan.FromSeconds(2);
                });
            services
                .Configure<ForwardedHeadersOptions>(x =>
                {
                    x.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    x.KnownIPNetworks.Clear();
                    x.KnownProxies.Clear();
                    foreach (var network in AppConfiguration.TrustedNetworks.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        x.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
                });
        })
        .Configure(app => app
            .UseForwardedHeaders()
            .MapWhen(
                context => context.Connection.LocalPort == AppConfiguration.TrapPort,
                trap => trap.Run(context => context
                    .RequestServices
                    .GetRequiredService<TrapHandler>()
                    .HandleAsync(context)))
            .UseRouting()
            .UseEndpoints(endpoints => endpoints.MapMetrics())
            .UseHealthChecks("/health"))
        .UseUrls($"http://+:{AppConfiguration.TrapPort}", $"http://+:{AppConfiguration.ManagementPort}"))
    .Build()
    .RunAsync();
