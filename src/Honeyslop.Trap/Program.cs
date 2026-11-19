using System.Runtime.CompilerServices;
using Honeyslop.Common;
using Honeyslop.Data;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Requests;
using Honeyslop.Llm;
using Honeyslop.Trap.Caching;
using Honeyslop.Trap.Caching.Internals;
using Honeyslop.Trap.Configuration;
using Honeyslop.Trap.Generation;
using Honeyslop.Trap.Generation.Internals;
using Honeyslop.Trap.Handlers;
using Honeyslop.Trap.Handlers.Internals;
using Honeyslop.Trap.Hits;
using Honeyslop.Trap.Hits.Internals;
using Honeyslop.Trap.Llm;
using Honeyslop.Trap.Llm.Internals;
using Honeyslop.Trap.Options;
using Honeyslop.Trap.Requests;
using Honeyslop.Trap.Requests.Internals;
using Honeyslop.Trap.Responses;
using Honeyslop.Trap.Responses.Internals;
using Honeyslop.Trap.Settings;
using Honeyslop.Trap.Settings.Internals;
using Microsoft.AspNetCore.HttpOverrides;
using Prometheus;
using IPNetwork = System.Net.IPNetwork;

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
                .AddSingleton<IEntryWatcher, EntryWatcher>()
                .AddSingleton<IHostedService>(serviceProvider => (IHostedService)serviceProvider.GetRequiredService<IEntryWatcher>())
                .AddSingleton<IHitCounter, HitCounter>()
                .AddSingleton<ISettingsProvider, SettingsProvider>()
                .AddSingleton<IClientDelays, ClientDelays>()
                .AddSingleton<IRequestLog, RequestLog>()
                .AddHostedService<RequestLogWriter>()
                .AddScoped<ITrapHandler, TrapHandler>()
                .AddHostedService<GenerationWorker>()
                .AddHostedService<HitFlushService>()
                .AddHostedService(serviceProvider => ActivatorUtilities.CreateInstance<ChangeListener>(
                    serviceProvider,
                    AppConfiguration.DbConnectionString))
                .AddHealthChecks();
            services
                .AddOptions<TrapOptions>()
                .Configure(x =>
                {
                    x.InstanceName = AppConfiguration.InstanceName;
                    x.Workers = GenerationConfiguration.Workers;
                    x.MaxWaiters = GenerationConfiguration.MaxWaiters;
                    x.MaxAttempts = GenerationConfiguration.MaxAttempts;
                    x.MaxRequestBodyBytes = GenerationConfiguration.MaxRequestBodyBytes;
                    x.MemoryCacheDuration = TimeSpan.FromSeconds(GenerationConfiguration.MemoryCacheSeconds);
                    x.MemoryCacheSizeLimit = GenerationConfiguration.MemoryCacheMegabytes * 1024L * 1024L;
                    x.HitFlushInterval = TimeSpan.FromSeconds(GenerationConfiguration.HitFlushSeconds);
                    x.Requests.Apply(GenerationConfiguration.MaxTargetLength, GenerationConfiguration.StaticExtensions);
                    x.ResponseDelay = TimeSpan.FromMilliseconds(Math.Clamp(GenerationConfiguration.ResponseDelayMilliseconds, 0, Entry.ResponseDelayMaxMilliseconds));
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
                        x.KnownIPNetworks.Add(IPNetwork.Parse(network));
                });
        })
        .Configure(app => app
            .UseForwardedHeaders()
            .MapWhen(
                context => context.Connection.LocalPort == AppConfiguration.TrapPort,
                trap => trap.Run(context => context
                    .RequestServices
                    .GetRequiredService<ITrapHandler>()
                    .HandleAsync(context)))
            .UseRouting()
            .UseEndpoints(endpoints => endpoints.MapMetrics())
            .UseHealthChecks("/health"))
        .UseUrls($"http://+:{AppConfiguration.TrapPort}", $"http://+:{AppConfiguration.ManagementPort}"))
    .Build()
    .RunAsync();