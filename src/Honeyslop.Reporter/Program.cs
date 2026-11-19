using System.Runtime.CompilerServices;
using Honeyslop.Common;
using Honeyslop.Data;
using Honeyslop.Reporter.Bounces;
using Honeyslop.Reporter.Configuration;
using Honeyslop.Reporter.Mail;
using Honeyslop.Reporter.Mail.Internals;
using Honeyslop.Reporter.Monitoring;
using Honeyslop.Reporter.Reports;
using Honeyslop.Reports;
using Prometheus;

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
                .AddSingleton<IMailSender, SmtpMailSender>()
                .AddSingleton<IMailbox, ImapMailbox>()
                .AddRequestArchives()
                .AddHostedService<ReportWorker>()
                .AddHostedService<BounceWorker>()
                .AddHostedService<ReportStateMetrics>()
                .AddHealthChecks();
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
                .AddOptions<ReporterOptions>()
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