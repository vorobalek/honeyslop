using System.Runtime.CompilerServices;
using Honeyslop.Common;
using Honeyslop.Data;
using Honeyslop.Llm;
using Honeyslop.Reports;
using Honeyslop.Worker;
using Honeyslop.Worker.Bounces;
using Honeyslop.Worker.Configuration;
using Honeyslop.Worker.Delays;
using Honeyslop.Worker.Generation;
using Honeyslop.Worker.Generation.Internals;
using Honeyslop.Worker.Llm;
using Honeyslop.Worker.Llm.Internals;
using Honeyslop.Worker.Mail;
using Honeyslop.Worker.Mail.Internals;
using Honeyslop.Worker.Monitoring;
using Honeyslop.Worker.Reports;
using Honeyslop.Worker.Responses;
using Honeyslop.Worker.Responses.Internals;
using Prometheus;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

Metrics.SuppressDefaultMetrics();
await Host
    .CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(builder => builder
        .AddGlobalCancellationToken()
        .AddJsonConsoleLogging()
        .AddDatabase(AppConfiguration.DbConnectionString)
        .AddSettingsCache()
        .ConfigureKestrel(options => options.AddServerHeader = false)
        .ConfigureServices(services =>
        {
            services
                .AddSingleton(TimeProvider.System)
                .AddSingleton<IMailSender, SmtpMailSender>()
                .AddSingleton<IMailbox, ImapMailbox>()
                .AddSingleton<IResponseSanitizer, ResponseSanitizer>()
                .AddSingleton<IPromptBuilder, PromptBuilder>()
                .AddSingleton<ILlmDispatcher, LlmDispatcher>()
                .AddSingleton<IGenerationSignal, GenerationSignal>()
                .AddRequestArchives()
                .AddHostedService<ReportWorker>()
                .AddHostedService<BounceWorker>()
                .AddHostedService<ReportStateMetrics>()
                .AddHostedService<GenerationWorker>()
                .AddHostedService<DelayLearningWorker>()
                .AddHostedService(serviceProvider => ActivatorUtilities.CreateInstance<ChangeListener>(
                    serviceProvider,
                    AppConfiguration.DbConnectionString))
                .AddHealthChecks();
            services
                .AddOptions<WorkerOptions>()
                .Configure(x =>
                {
                    x.InstanceName = AppConfiguration.InstanceName;
                    x.GenerationWorkers = GenerationConfiguration.Workers;
                    x.MaxAttempts = GenerationConfiguration.MaxAttempts;
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
                    x.TopP = LlmConfiguration.TopP;
                    x.TopK = LlmConfiguration.TopK;
                    x.MinP = LlmConfiguration.MinP;
                    x.Timeout = Timeout.InfiniteTimeSpan;
                    x.DiscoveryInterval = TimeSpan.FromSeconds(LlmConfiguration.DiscoverySeconds);
                    x.SlotPollInterval = TimeSpan.FromMilliseconds(LlmConfiguration.SlotPollMilliseconds);
                    x.ProbeTimeout = TimeSpan.FromSeconds(2);
                    x.UnhealthyCooldown = TimeSpan.FromSeconds(10);
                    x.DispatchGrace = TimeSpan.FromSeconds(2);
                });
            services
                .AddOptions<SmtpOptions>()
                .Configure(x =>
                {
                    x.Host = AppConfiguration.SmtpHost;
                    x.Port = AppConfiguration.SmtpPort;
                    x.Username = AppConfiguration.SmtpUsername;
                    x.Password = AppConfiguration.SmtpPassword;
                    x.From = AppConfiguration.SmtpFrom;
                    x.StartTls = AppConfiguration.SmtpStartTls;
                });
            services
                .AddOptions<ImapOptions>()
                .Configure(x =>
                {
                    x.Host = AppConfiguration.ImapHost;
                    x.Port = AppConfiguration.ImapPort;
                    x.Username = AppConfiguration.ImapUsername;
                    x.Password = AppConfiguration.ImapPassword;
                });
            services
                .AddOptions<BounceOptions>()
                .Configure(x =>
                {
                    x.Folders = AppConfiguration.ImapFolders.Length > 0 ? AppConfiguration.ImapFolders : ["INBOX"];
                    x.PollInterval = TimeSpan.FromSeconds(Math.Max(10, AppConfiguration.ImapPollSeconds));
                });
            services
                .AddOptions<ReportOptions>()
                .Configure(x =>
                {
                    x.InstanceName = AppConfiguration.InstanceName;
                    x.MaxAttachedRequests = AppConfiguration.MaxAttachedRequests;
                });
        })
        .Configure(app => app
            .UseRouting()
            .UseEndpoints(endpoints => endpoints.MapMetrics())
            .UseHealthChecks("/health"))
        .UseUrls($"http://+:{AppConfiguration.ManagementPort}"))
    .Build()
    .RunAsync();