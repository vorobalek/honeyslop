using Honeyslop.Admin.Access;
using Honeyslop.Admin.Services;
using Honeyslop.Data.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Honeyslop.Admin.Pages;

public sealed class SettingsModel(
    ISettingsRepository settingsRepository,
    ICacheMaintenance cacheMaintenance,
    IPublicView publicView,
    TimeProvider timeProvider) : PageModel
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

    [BindProperty(SupportsGet = true)]
    public bool Saved { get; set; }

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
        var settings = await settingsRepository.GetAsync(cancellationToken);
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
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (CacheTtlValue < 0)
            ModelState.AddModelError(nameof(CacheTtlValue), "Must be zero or positive.");
        if (KeepUnusedValue < 0)
            ModelState.AddModelError(nameof(KeepUnusedValue), "Must be zero or positive.");
        if (Join(ClaimTimeoutValue, ClaimTimeoutUnit) < TimeSpan.FromSeconds(20))
            ModelState.AddModelError(nameof(ClaimTimeoutValue), "Must be at least 20 seconds.");
        if (RetryFailedValue < 0)
            ModelState.AddModelError(nameof(RetryFailedValue), "Must be zero or positive.");
        if (Join(TimeLimitValue, TimeLimitUnit) < TimeSpan.FromSeconds(10))
            ModelState.AddModelError(nameof(TimeLimitValue), "Must be at least 10 seconds.");
        if (GenerationWaitValue < 0)
            ModelState.AddModelError(nameof(GenerationWaitValue), "Must be zero or positive.");
        if (RequestRetentionValue < 0)
            ModelState.AddModelError(nameof(RequestRetentionValue), "Must be zero or positive.");
        if (!ModelState.IsValid)
            return Page();

        var settings = new HoneyslopSettings(
            Join(CacheTtlValue, CacheTtlUnit),
            Join(KeepUnusedValue, KeepUnusedUnit),
            GenerationEnabled,
            Join(RequestRetentionValue, RequestRetentionUnit),
            Join(GenerationWaitValue, GenerationWaitUnit),
            AllowRefusals,
            Join(ClaimTimeoutValue, ClaimTimeoutUnit),
            Join(RetryFailedValue, RetryFailedUnit),
            Join(TimeLimitValue, TimeLimitUnit),
            PublicView);
        await settingsRepository.SaveAsync(settings, timeProvider.GetUtcNow(), cancellationToken);
        publicView.Reset();
        var result = await cacheMaintenance.RunAsync(settings, cancellationToken);
        return RedirectToPage(new
        {
            Saved = true,
            result.Expired,
            result.Unused,
            result.Pruned,
            result.Deleted
        });
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
