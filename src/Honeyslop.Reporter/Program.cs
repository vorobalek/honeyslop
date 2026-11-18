using System.Runtime.CompilerServices;
using Honeyslop.Common;
using Honeyslop.Data;
using Honeyslop.Reporter.Configuration;
using Honeyslop.Reporter.Mail;
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
                .AddRequestArchives()
                .AddHostedService<ReportWorker>()
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
