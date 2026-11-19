using System.Security.Claims;
using Honeyslop.Admin.Access;
using Honeyslop.Admin.Configuration;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Admin.Live;
using Honeyslop.Admin.Live.Contracts;
using Honeyslop.Admin.Live.Contracts.Watches;
using Honeyslop.Admin.Live.Enums;
using Honeyslop.Admin.Views.Contracts;
using Honeyslop.Data.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace Honeyslop.Admin.Hubs;

public sealed class AdminHub(
    ILiveSubscriptions subscriptions,
    IWatchHandlers watches,
    IEntriesService entriesService,
    IClientsService clientsService,
    ISettingsService settingsService,
    IBlockedContactsService blockedContactsService) : Hub
{
    private ClaimsPrincipal User => Context.User ?? new ClaimsPrincipal();

    private string Audience => User.CanOperate ? "operators" : "anonymous";

    private string PersonalAudience => User.CanOperate ? $"operator {User.UserId}" : "anonymous";

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

    [HubMethodName("watch")]
    public Task WatchAsync(string watch, WatchRequest request)
    {
        var handler = watches.For(request);
        var audience = handler.Audience == WatchAudience.Personal ? PersonalAudience : Audience;
        return subscriptions.WatchAsync(
            new LiveWatch(
                Context.ConnectionId,
                watch,
                audience,
                request,
                User,
                handler.InterestIn(request),
                (views, cancellationToken) => handler.BuildAsync(views, request, cancellationToken)),
            Context.ConnectionAborted);
    }

    [HubMethodName("unwatch")]
    public Task UnwatchAsync(string watch)
    {
        return subscriptions.UnwatchAsync(Context.ConnectionId, watch, Context.ConnectionAborted);
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

    [HubMethodName("createEntry")]
    public Task<EntryChangeDto> CreateEntryAsync(EntryRequestDto request, string? guidance)
    {
        return entriesService.CreateAsync(request, guidance, Context.ConnectionAborted);
    }

    [HubMethodName("createEntryWithResponse")]
    public Task<EntryChangeDto> CreateEntryWithResponseAsync(EntryRequestDto request, EntryResponseDto response)
    {
        return entriesService.CreateWithResponseAsync(request, response, Context.ConnectionAborted);
    }

    [HubMethodName("setClientDelay")]
    public Task<ClientDelayChangeDto> SetClientDelayAsync(string ip, int? milliseconds)
    {
        return clientsService.SetResponseDelayAsync(ip, milliseconds, Context.ConnectionAborted);
    }

    [HubMethodName("setEntryDelay")]
    public Task<EntryChangeDto> SetEntryDelayAsync(long[] ids, int? milliseconds)
    {
        return entriesService.SetResponseDelayAsync(ids, milliseconds, Context.ConnectionAborted);
    }

    [HubMethodName("report")]
    public Task<ReportRequestDto> ReportAsync(string ip, string? senderName)
    {
        return clientsService.ReportAsync(ip, senderName, Context.ConnectionAborted);
    }

    [HubMethodName("blockContacts")]
    public Task<ContactsChangeDto> BlockContactsAsync(string[] contacts, string? note)
    {
        return blockedContactsService.BlockAsync(contacts, note, Context.ConnectionAborted);
    }

    [HubMethodName("unblockContacts")]
    public Task<ContactsChangeDto> UnblockContactsAsync(string[] contacts)
    {
        return blockedContactsService.UnblockAsync(contacts, Context.ConnectionAborted);
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
                UserAgent = string.IsNullOrWhiteSpace(form.UserAgent) ? null : form.UserAgent.Trim(),
                ResponseDelayMilliseconds = form.ResponseDelayMilliseconds
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
}