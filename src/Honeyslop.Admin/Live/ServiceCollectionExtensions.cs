using Honeyslop.Admin.Live.Contracts;
using Honeyslop.Admin.Live.Contracts.Watches;
using Honeyslop.Admin.Live.Enums;
using Honeyslop.Admin.Live.Internals;
using Honeyslop.Admin.Views;

namespace Honeyslop.Admin.Live;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddLiveWatches()
        {
            return services
                .AddWatch<EntriesWatch, object>(
                    WatchAudience.Shared,
                    _ => new LiveInterest([LiveTopic.Entries]),
                    async (views, request, cancellationToken) => await views.GetEntriesAsync(request.Query, cancellationToken))
                .AddWatch<EntryWatch, object?>(
                    WatchAudience.Shared,
                    request => new LiveInterest([LiveTopic.Entries, LiveTopic.Outputs], request.Id),
                    async (views, request, cancellationToken) => await views.GetEntryAsync(request.Id, cancellationToken))
                .AddWatch<RequestsWatch, object>(
                    WatchAudience.Shared,
                    _ => new LiveInterest([LiveTopic.Requests]),
                    async (views, request, cancellationToken) => await views.GetRequestsAsync(request.Query, cancellationToken))
                .AddWatch<RequestWatch, object?>(
                    WatchAudience.Shared,
                    _ => LiveInterest.Nothing,
                    async (views, request, cancellationToken) => await views.GetRequestAsync(request.Id, cancellationToken))
                .AddWatch<ClientsWatch, object>(
                    WatchAudience.Shared,
                    _ => LiveInterest.Nothing,
                    async (views, request, cancellationToken) => await views.GetClientsAsync(request.Query, cancellationToken))
                .AddWatch<ClientWatch, object?>(
                    WatchAudience.Shared,
                    _ => LiveInterest.Nothing,
                    async (views, request, cancellationToken) => await views.GetClientAsync(request.Ip, cancellationToken))
                .AddWatch<ClientAbuseWatch, object?>(
                    WatchAudience.Personal,
                    _ => new LiveInterest([LiveTopic.Reports]),
                    async (views, request, cancellationToken) => await views.GetClientAbuseAsync(request.Ip, cancellationToken))
                .AddWatch<ReportsWatch, object>(
                    WatchAudience.Shared,
                    _ => new LiveInterest([LiveTopic.Reports]),
                    async (views, request, cancellationToken) => await views.GetReportsAsync(request.Query, cancellationToken))
                .AddWatch<ReportWatch, object?>(
                    WatchAudience.Shared,
                    _ => new LiveInterest([LiveTopic.Reports]),
                    async (views, request, cancellationToken) => await views.GetReportAsync(request.Id, cancellationToken))
                .AddWatch<GeneratorsWatch, object>(
                    WatchAudience.Shared,
                    _ => new LiveInterest([LiveTopic.Entries], Periodic: true),
                    async (views, _, cancellationToken) => await views.GetGeneratorsAsync(cancellationToken))
                .AddWatch<SettingsWatch, object>(
                    WatchAudience.Personal,
                    _ => LiveInterest.Nothing,
                    async (views, _, cancellationToken) => await views.GetSettingsAsync(cancellationToken))
                .AddSingleton<IWatchHandlers, WatchHandlers>();
        }

        private IServiceCollection AddWatch<TRequest, TView>(
            WatchAudience audience,
            Func<TRequest, LiveInterest> interest,
            Func<IViewBuilder, TRequest, CancellationToken, Task<TView>> build)
            where TRequest : WatchRequest
        {
            return services.AddSingleton<IWatchHandler>(new WatchHandler<TRequest, TView>(audience, interest, build));
        }
    }
}