using System.Security.Claims;
using Honeyslop.Admin.Access;
using Honeyslop.Admin.Configuration;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Admin.Live;
using Honeyslop.Admin.Live.Contracts;
using Honeyslop.Admin.Live.Enums;
using Honeyslop.Admin.Views;
using Honeyslop.Admin.Views.Contracts;
using Honeyslop.Data.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace Honeyslop.Admin.Hubs;

public sealed class AdminHub(
    ILiveSubscriptions subscriptions,
    IEntriesService entriesService,
    IClientsService clientsService,
    ISettingsService settingsService) : Hub
{
    private ClaimsPrincipal User => Context.User ?? new ClaimsPrincipal();

    [HubMethodName("session")]
    public SessionDto GetSession()
    {
        return new SessionDto(
            AppConfiguration.SignInEnabled,
            User.Identity?.IsAuthenticated == true,
            User.DisplayName,
            User.UserId,
            User.CanOperate);
    }

    [HubMethodName("watchEntries")]
    public void WatchEntries(int watch, EntriesQuery query)
    {
        Watch(watch, new LiveInterest([LiveTopic.Entries]), async (views, cancellationToken) =>
            await views.GetEntriesAsync(query, cancellationToken));
    }

    [HubMethodName("watchEntry")]
    public void WatchEntry(int watch, long id)
    {
        Watch(watch, new LiveInterest([LiveTopic.Entries, LiveTopic.Outputs], id), async (views, cancellationToken) =>
            await views.GetEntryAsync(id, cancellationToken));
    }

    [HubMethodName("watchRequests")]
    public void WatchRequests(int watch, RequestsQuery query)
    {
        Watch(watch, new LiveInterest([LiveTopic.Requests]), async (views, cancellationToken) =>
            await views.GetRequestsAsync(query, cancellationToken));
    }

    [HubMethodName("watchRequest")]
    public void WatchRequest(int watch, long id)
    {
        Watch(watch, LiveInterest.Nothing, async (views, cancellationToken) =>
            await views.GetRequestAsync(id, cancellationToken));
    }

    [HubMethodName("watchClients")]
    public void WatchClients(int watch, ClientsQuery query)
    {
        Watch(watch, LiveInterest.Nothing, async (views, cancellationToken) =>
            await views.GetClientsAsync(query, cancellationToken));
    }

    [HubMethodName("watchClient")]
    public void WatchClient(int watch, string ip)
    {
        Watch(watch, LiveInterest.Nothing, async (views, cancellationToken) =>
            await views.GetClientAsync(ip, cancellationToken));
    }

    [HubMethodName("watchClientAbuse")]
    public void WatchClientAbuse(int watch, string ip)
    {
        Watch(watch, new LiveInterest([LiveTopic.Reports]), async (views, cancellationToken) =>
            await views.GetClientAbuseAsync(ip, cancellationToken));
    }

    [HubMethodName("watchReports")]
    public void WatchReports(int watch, ReportsQuery query)
    {
        Watch(watch, new LiveInterest([LiveTopic.Reports]), async (views, cancellationToken) =>
            await views.GetReportsAsync(query, cancellationToken));
    }

    [HubMethodName("watchReport")]
    public void WatchReport(int watch, long id)
    {
        Watch(watch, new LiveInterest([LiveTopic.Reports]), async (views, cancellationToken) =>
            await views.GetReportAsync(id, cancellationToken));
    }

    [HubMethodName("watchGenerators")]
    public void WatchGenerators(int watch)
    {
        Watch(watch, new LiveInterest([LiveTopic.Entries], Periodic: true), async (views, cancellationToken) =>
            await views.GetGeneratorsAsync(cancellationToken));
    }

    [HubMethodName("watchSettings")]
    public void WatchSettings(int watch)
    {
        Watch(watch, LiveInterest.Nothing, async (views, cancellationToken) =>
            await views.GetSettingsAsync(cancellationToken));
    }

    [HubMethodName("unwatch")]
    public void Unwatch(int watch)
    {
        subscriptions.Unwatch(Context.ConnectionId, watch);
    }

    [HubMethodName("expireEntries")]
    public Task<int> ExpireEntriesAsync(long[] ids)
    {
        return entriesService.ExpireAsync(ids, Context.ConnectionAborted);
    }

    [HubMethodName("expireMatching")]
    public Task<int> ExpireMatchingAsync(EntryFilter filter)
    {
        return entriesService.ExpireMatchingAsync(filter, Context.ConnectionAborted);
    }

    [HubMethodName("regenerateEntries")]
    public Task<EntryChangeDto> RegenerateEntriesAsync(long[] ids, string? guidance)
    {
        return entriesService.RegenerateAsync(ids, guidance, Context.ConnectionAborted);
    }

    [HubMethodName("replaceEntries")]
    public Task<EntryChangeDto> ReplaceEntriesAsync(long[] ids, EntryResponseDto response)
    {
        return entriesService.ReplaceAsync(ids, response, Context.ConnectionAborted);
    }

    [HubMethodName("report")]
    public Task<ReportRequestDto> ReportAsync(string ip, string? senderName)
    {
        return clientsService.ReportAsync(ip, senderName, Context.ConnectionAborted);
    }

    [HubMethodName("saveSettings")]
    public Task<SettingsSaveDto> SaveSettingsAsync(SettingsFormDto form)
    {
        return settingsService.SaveAsync(
            new HoneyslopSettings
            {
                CacheTtl = TimeSpan.FromSeconds(form.CacheTtlSeconds),
                KeepUnused = TimeSpan.FromSeconds(form.KeepUnusedSeconds),
                GenerationEnabled = form.GenerationEnabled,
                RequestRetention = TimeSpan.FromSeconds(form.RequestRetentionSeconds),
                GenerationWait = TimeSpan.FromSeconds(form.GenerationWaitSeconds),
                AllowRefusals = form.AllowRefusals,
                ClaimTimeout = TimeSpan.FromSeconds(form.ClaimTimeoutSeconds),
                RetryFailedAfter = TimeSpan.FromSeconds(form.RetryFailedSeconds),
                GenerationTimeLimit = TimeSpan.FromSeconds(form.GenerationTimeLimitSeconds),
                PublicView = form.PublicView,
                UserAgent = string.IsNullOrWhiteSpace(form.UserAgent) ? null : form.UserAgent.Trim()
            },
            Context.ConnectionAborted);
    }

    [HubMethodName("savePersonal")]
    public Task<string?> SavePersonalAsync(string? senderName)
    {
        return settingsService.SavePersonalAsync(senderName, Context.ConnectionAborted);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        subscriptions.Drop(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    private void Watch(int watch, LiveInterest interest, Func<IViewBuilder, CancellationToken, Task<object?>> build)
    {
        subscriptions.Watch(Context.ConnectionId, watch, User, interest, build);
    }
}
