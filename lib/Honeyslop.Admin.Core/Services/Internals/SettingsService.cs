using Honeyslop.Admin.Core.Access;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Exceptions;
using Honeyslop.Admin.Core.Maintenance;
using Honeyslop.Admin.Core.Options;
using Honeyslop.Admin.Core.Text;
using Honeyslop.Data.Contracts;
using Honeyslop.Data.Repositories;
using Microsoft.Extensions.Options;

namespace Honeyslop.Admin.Core.Services.Internals;

internal sealed class SettingsService(
    ISettingsRepository settingsRepository,
    ICacheMaintenance cacheMaintenance,
    IPublicView publicView,
    IOptions<AbuseReportOptions> reportOptions,
    IOptions<UserAgentOptions> userAgentOptions,
    IAccessGuard access,
    IServerTimeRepository serverTime) : ISettingsService
{
    public async Task<SettingsDto> GetAsync(CancellationToken cancellationToken)
    {
        await access.EnsureCanReadAsync(cancellationToken);
        var viewer = access.Viewer;
        var settings = await settingsRepository.GetAsync(viewer.UserId, cancellationToken);
        return new SettingsDto(
            settings.Honeyslop,
            userAgentOptions.Value.Default,
            viewer.CanOperate ? reportOptions.Value.DefaultSenderName : null,
            settings.User?.SenderName);
    }

    public async Task<SettingsSaveDto> SaveAsync(HoneyslopSettings settings, CancellationToken cancellationToken)
    {
        access.EnsureCanOperate();
        settings = settings with { UserAgent = string.IsNullOrWhiteSpace(settings.UserAgent) ? null : settings.UserAgent.Trim() };
        var errors = Validate(settings);
        if (errors.Count > 0)
            return new SettingsSaveDto(errors, null);

        await settingsRepository.SaveAsync(settings, await serverTime.GetNowAsync(cancellationToken), cancellationToken);
        publicView.Reset();
        return new SettingsSaveDto(errors, await cacheMaintenance.RunAsync(settings, cancellationToken));
    }

    public async Task<string?> SavePersonalAsync(string? senderName, CancellationToken cancellationToken)
    {
        access.EnsureCanOperate();
        if (access.Viewer.UserId is not { } userId)
            throw new AccessDeniedException("Personal settings need a signed-in user.");
        var cleaned = SenderName.Clean(senderName);
        await settingsRepository.SaveAsync(new UserSettings { UserId = userId, SenderName = cleaned }, await serverTime.GetNowAsync(cancellationToken), cancellationToken);
        return cleaned;
    }

    private static Dictionary<string, string> Validate(HoneyslopSettings settings)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        if (settings.CacheTtl < TimeSpan.Zero)
            errors[nameof(settings.CacheTtl)] = "Must be zero or positive.";
        if (settings.KeepUnused < TimeSpan.Zero)
            errors[nameof(settings.KeepUnused)] = "Must be zero or positive.";
        if (settings.ClaimTimeout < TimeSpan.FromSeconds(20))
            errors[nameof(settings.ClaimTimeout)] = "Must be at least 20 seconds.";
        if (settings.RetryFailedAfter < TimeSpan.Zero)
            errors[nameof(settings.RetryFailedAfter)] = "Must be zero or positive.";
        if (settings.GenerationTimeLimit < TimeSpan.FromSeconds(10))
            errors[nameof(settings.GenerationTimeLimit)] = "Must be at least 10 seconds.";
        if (settings.GenerationWait < TimeSpan.Zero)
            errors[nameof(settings.GenerationWait)] = "Must be zero or positive.";
        if (settings.RequestRetention < TimeSpan.Zero)
            errors[nameof(settings.RequestRetention)] = "Must be zero or positive.";
        if (settings.UserAgent is not null && !UserAgentHeader.IsValid(settings.UserAgent))
            errors[nameof(settings.UserAgent)] = "Must be a valid User-Agent header of at most 512 characters.";
        return errors;
    }
}
