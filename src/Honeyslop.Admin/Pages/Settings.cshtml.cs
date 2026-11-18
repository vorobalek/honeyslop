using Honeyslop.Admin.Access;
using Honeyslop.Admin.Core.Contracts;
using Honeyslop.Admin.Core.Services;
using Honeyslop.Data.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Honeyslop.Admin.Pages;

public sealed class SettingsModel(ISettingsService settingsService) : PageModel
{
    [BindProperty]
    public long CacheTtlValue { get; set; }

    [BindProperty]
    public DurationUnit CacheTtlUnit { get; set; }

    [BindProperty]
    public long KeepUnusedValue { get; set; }

    [BindProperty]
    public DurationUnit KeepUnusedUnit { get; set; }

    [BindProperty]
    public bool GenerationEnabled { get; set; }

    [BindProperty]
    public bool AllowRefusals { get; set; }

    [BindProperty]
    public long ClaimTimeoutValue { get; set; }

    [BindProperty]
    public DurationUnit ClaimTimeoutUnit { get; set; }

    [BindProperty]
    public long RetryFailedValue { get; set; }

    [BindProperty]
    public DurationUnit RetryFailedUnit { get; set; }

    [BindProperty]
    public long TimeLimitValue { get; set; }

    [BindProperty]
    public DurationUnit TimeLimitUnit { get; set; }

    [BindProperty]
    public long GenerationWaitValue { get; set; }

    [BindProperty]
    public DurationUnit GenerationWaitUnit { get; set; }

    [BindProperty]
    public long RequestRetentionValue { get; set; }

    [BindProperty]
    public DurationUnit RequestRetentionUnit { get; set; }

    [BindProperty]
    public bool PublicView { get; set; }

    [BindProperty]
    public string? UserAgent { get; set; }

    [BindProperty]
    public string? PersonalSenderName { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Saved { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool PersonalSaved { get; set; }

    public string? DefaultSenderName { get; private set; }
    public string DefaultUserAgent { get; private set; } = string.Empty;
    public string DefaultSenderNameHint => DefaultSenderName is null ? "the address alone" : $"\"{DefaultSenderName}\"";

    [BindProperty(SupportsGet = true)]
    public int Expired { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Unused { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Pruned { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Deleted { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var view = await LoadAsync(cancellationToken);
        var settings = view.Settings;
        (CacheTtlValue, CacheTtlUnit) = Split(settings.CacheTtl);
        (KeepUnusedValue, KeepUnusedUnit) = Split(settings.KeepUnused);
        GenerationEnabled = settings.GenerationEnabled;
        AllowRefusals = settings.AllowRefusals;
        (ClaimTimeoutValue, ClaimTimeoutUnit) = Split(settings.ClaimTimeout);
        (RetryFailedValue, RetryFailedUnit) = Split(settings.RetryFailedAfter);
        (TimeLimitValue, TimeLimitUnit) = Split(settings.GenerationTimeLimit);
        (RequestRetentionValue, RequestRetentionUnit) = Split(settings.RequestRetention);
        (GenerationWaitValue, GenerationWaitUnit) = Split(settings.GenerationWait);
        PublicView = settings.PublicView;
        UserAgent = settings.UserAgent;
        PersonalSenderName = view.PersonalSenderName;
    }

    public async Task<IActionResult> OnPostPersonalAsync(CancellationToken cancellationToken)
    {
        if (User.UserId is null)
            return Forbid();
        await settingsService.SavePersonalAsync(PersonalSenderName, cancellationToken);
        return RedirectToPage(new { PersonalSaved = true });
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        UserAgent = string.IsNullOrWhiteSpace(UserAgent) ? null : UserAgent.Trim();
        var settings = new HoneyslopSettings
        {
            CacheTtl = Join(CacheTtlValue, CacheTtlUnit),
            KeepUnused = Join(KeepUnusedValue, KeepUnusedUnit),
            GenerationEnabled = GenerationEnabled,
            RequestRetention = Join(RequestRetentionValue, RequestRetentionUnit),
            GenerationWait = Join(GenerationWaitValue, GenerationWaitUnit),
            AllowRefusals = AllowRefusals,
            ClaimTimeout = Join(ClaimTimeoutValue, ClaimTimeoutUnit),
            RetryFailedAfter = Join(RetryFailedValue, RetryFailedUnit),
            GenerationTimeLimit = Join(TimeLimitValue, TimeLimitUnit),
            PublicView = PublicView,
            UserAgent = UserAgent
        };
        var save = await settingsService.SaveAsync(settings, cancellationToken);
        if (save.Maintenance is not { } result)
        {
            foreach (var (field, error) in save.Errors)
                ModelState.AddModelError(Fields.GetValueOrDefault(field, field), error);
            await LoadAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage(new
        {
            Saved = true,
            result.Expired,
            result.Unused,
            result.Pruned,
            result.Deleted
        });
    }

    private static readonly Dictionary<string, string> Fields = new(StringComparer.Ordinal)
    {
        [nameof(HoneyslopSettings.CacheTtl)] = nameof(CacheTtlValue),
        [nameof(HoneyslopSettings.KeepUnused)] = nameof(KeepUnusedValue),
        [nameof(HoneyslopSettings.ClaimTimeout)] = nameof(ClaimTimeoutValue),
        [nameof(HoneyslopSettings.RetryFailedAfter)] = nameof(RetryFailedValue),
        [nameof(HoneyslopSettings.GenerationTimeLimit)] = nameof(TimeLimitValue),
        [nameof(HoneyslopSettings.GenerationWait)] = nameof(GenerationWaitValue),
        [nameof(HoneyslopSettings.RequestRetention)] = nameof(RequestRetentionValue),
        [nameof(HoneyslopSettings.UserAgent)] = nameof(UserAgent)
    };

    private async Task<SettingsDto> LoadAsync(CancellationToken cancellationToken)
    {
        var view = await settingsService.GetAsync(cancellationToken);
        DefaultSenderName = view.DefaultSenderName;
        DefaultUserAgent = view.DefaultUserAgent;
        return view;
    }

    private static (long Value, DurationUnit Unit) Split(TimeSpan duration)
    {
        var seconds = (long)duration.TotalSeconds;
        if (seconds == 0)
            return (0, DurationUnit.Days);
        if (seconds % 60 != 0)
            return (seconds, DurationUnit.Seconds);
        var minutes = seconds / 60;
        if (minutes % (24 * 60) == 0)
            return (minutes / (24 * 60), DurationUnit.Days);
        return minutes % 60 == 0
            ? (minutes / 60, DurationUnit.Hours)
            : (minutes, DurationUnit.Minutes);
    }

    private static TimeSpan Join(long value, DurationUnit unit)
    {
        return unit switch
        {
            DurationUnit.Seconds => TimeSpan.FromSeconds(value),
            DurationUnit.Minutes => TimeSpan.FromMinutes(value),
            DurationUnit.Hours => TimeSpan.FromHours(value),
            _ => TimeSpan.FromDays(value)
        };
    }

    public enum DurationUnit
    {
        Seconds,
        Minutes,
        Hours,
        Days
    }
}
