using System.ComponentModel;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Services;
using ModelContextProtocol.Server;

namespace Honeyslop.Admin.Mcp.Tools;

[McpServerToolType]
public sealed class Settings(ISettingsService settingsService)
{
    [McpServerTool(Name = "get_settings", Title = "Get the settings", ReadOnly = true, Idempotent = true)]
    [Description("Runtime settings: cache lifetimes, generation limits, request retention, the public view, the outbound User-Agent, the default response delay, and the sender names for abuse reports.")]
    public Task<SettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        return settingsService.GetAsync(cancellationToken);
    }

    [McpServerTool(Name = "update_settings", Title = "Change the settings", Destructive = true, Idempotent = true)]
    [Description(
        "Changes the given runtime settings and keeps the rest. Durations are in seconds, 0 means never or off where the setting allows it. Saving applies them at once: entries past the new lifetimes expire and requests past the retention are deleted. Returns validation errors or what the cleanup did.")]
    public async Task<SettingsSaveDto> UpdateAsync(
        [Description("Expire generated responses after this long.")]
        long? cacheTtlSeconds = null,
        [Description("Expire responses nobody requested for this long.")]
        long? keepUnusedSeconds = null,
        [Description("Generate responses for new paths.")]
        bool? generationEnabled = null,
        [Description("Delete recorded requests older than this.")]
        long? requestRetentionSeconds = null,
        [Description("How long a request waits for its response to be generated.")]
        long? generationWaitSeconds = null,
        [Description("Let the model refuse to answer.")]
        bool? allowRefusals = null,
        [Description("Take a generation over when its worker stops sending heartbeats for this long, at least 20.")]
        long? claimTimeoutSeconds = null,
        [Description("Retry failed generations after this long.")]
        long? retryFailedSeconds = null,
        [Description("Give up a generation after this long, at least 10.")]
        long? generationTimeLimitSeconds = null,
        [Description("Let anyone browse the admin read-only.")]
        bool? publicView = null,
        [Description("User-Agent for outbound lookups; an empty string restores the default.")]
        string? userAgent = null,
        [Description("Milliseconds after a request arrives before the trap answers it from an entry when neither the client nor the entry sets a delay of its own, 0 to 600000; time spent generating counts towards it; -1 goes back to RESPONSE_DELAY_MILLISECONDS.")]
        int? responseDelayMilliseconds = null,
        [Description(
            "Auto-Delay: the trap learns for every client address how long it waits before dropping the connection and holds it just below that, within the range; a new address starts in the middle and responses trickle so the last byte leaves at the client's delay. A delay set on a client by hand wins.")]
        bool? autoDelayEnabled = null,
        [Description("Shortest Auto-Delay in milliseconds, 0 or more.")]
        int? autoDelayMinMilliseconds = null,
        [Description("Longest Auto-Delay in milliseconds, at most 600000.")]
        int? autoDelayMaxMilliseconds = null,
        CancellationToken cancellationToken = default)
    {
        var current = (await settingsService.GetAsync(cancellationToken)).Settings;
        var settings = current with
        {
            CacheTtl = Seconds(cacheTtlSeconds) ?? current.CacheTtl,
            KeepUnused = Seconds(keepUnusedSeconds) ?? current.KeepUnused,
            GenerationEnabled = generationEnabled ?? current.GenerationEnabled,
            RequestRetention = Seconds(requestRetentionSeconds) ?? current.RequestRetention,
            GenerationWait = Seconds(generationWaitSeconds) ?? current.GenerationWait,
            AllowRefusals = allowRefusals ?? current.AllowRefusals,
            ClaimTimeout = Seconds(claimTimeoutSeconds) ?? current.ClaimTimeout,
            RetryFailedAfter = Seconds(retryFailedSeconds) ?? current.RetryFailedAfter,
            GenerationTimeLimit = Seconds(generationTimeLimitSeconds) ?? current.GenerationTimeLimit,
            PublicView = publicView ?? current.PublicView,
            UserAgent = userAgent ?? current.UserAgent,
            ResponseDelayMilliseconds = responseDelayMilliseconds switch
            {
                null => current.ResponseDelayMilliseconds,
                -1 => null,
                { } milliseconds => milliseconds
            },
            AutoDelayEnabled = autoDelayEnabled ?? current.AutoDelayEnabled,
            AutoDelayMinMilliseconds = autoDelayMinMilliseconds ?? current.AutoDelayMinMilliseconds,
            AutoDelayMaxMilliseconds = autoDelayMaxMilliseconds ?? current.AutoDelayMaxMilliseconds
        };
        return await settingsService.SaveAsync(settings, cancellationToken);
    }

    [McpServerTool(Name = "set_sender_name", Title = "Set my sender name", Destructive = false, Idempotent = true)]
    [Description("Saves the name the signed-in operator's abuse reports are signed with; an empty name falls back to the default. Returns the name as saved.")]
    public Task<string?> SetSenderNameAsync([Description("Sender name.")] string? name, CancellationToken cancellationToken = default)
    {
        return settingsService.SavePersonalAsync(name, cancellationToken);
    }

    private static TimeSpan? Seconds(long? seconds)
    {
        return seconds is { } value ? TimeSpan.FromSeconds(value) : null;
    }
}