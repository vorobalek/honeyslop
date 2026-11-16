using System.Globalization;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;
using Honeyslop.Data.Repositories;

namespace Honeyslop.Admin.Extensions;

public static class DisplayExtensions
{
    extension(DateTimeOffset? value)
    {
        public string Display => value?.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "—";
    }

    extension(DateTimeOffset value)
    {
        public string Display => value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    extension(int bytes)
    {
        public string DisplaySize => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => string.Create(CultureInfo.InvariantCulture, $"{bytes / 1024d:0.#} KB"),
            _ => string.Create(CultureInfo.InvariantCulture, $"{bytes / 1024d / 1024d:0.#} MB")
        };
    }

    extension(RequestOutcome outcome)
    {
        public string CssClass => outcome switch
        {
            RequestOutcome.Hit => "badge badge-ready",
            RequestOutcome.Generated => "badge badge-generated",
            RequestOutcome.Fallback => "badge badge-pending",
            RequestOutcome.Abandoned => "badge badge-pending",
            RequestOutcome.Error => "badge badge-failed",
            RequestOutcome.Waiting => "badge badge-generated",
            _ => "badge badge-static"
        };
    }

    extension(EntryState state)
    {
        public string CssClass => state switch
        {
            EntryState.Ready => "badge badge-ready",
            EntryState.Generating => "badge badge-generated",
            EntryState.Queued => "badge badge-pending",
            EntryState.Expired => "badge badge-static",
            _ => "badge badge-failed"
        };

        public string Label => state.ToString().ToLowerInvariant();
    }

    extension(Entry entry)
    {
        public EntryState State => entry.Status switch
        {
            EntryStatus.Pending when entry.ClaimedBy is null => EntryState.Queued,
            EntryStatus.Pending => EntryState.Generating,
            EntryStatus.Ready => EntryState.Ready,
            EntryStatus.Expired => EntryState.Expired,
            _ => EntryState.Failed
        };
    }

    extension(TimeSpan duration)
    {
        public string DisplayDuration => duration.TotalSeconds switch
        {
            < 0 => "0 s",
            < 60 => $"{(int)duration.TotalSeconds} s",
            < 3600 => $"{(int)duration.TotalMinutes} min {duration.Seconds} s",
            _ => $"{(int)duration.TotalHours} h {duration.Minutes} min"
        };
    }
}
