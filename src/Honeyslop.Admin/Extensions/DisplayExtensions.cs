using System.Globalization;
using Honeyslop.Data.Enums;

namespace Honeyslop.Admin.Extensions;

public static class DisplayExtensions
{
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

    extension(AbuseReportState state)
    {
        public string CssClass => state switch
        {
            AbuseReportState.Sent => "badge badge-ready",
            AbuseReportState.Failed => "badge badge-failed",
            AbuseReportState.Sending => "badge badge-generated",
            _ => "badge badge-pending"
        };

        public string Label => state.ToString().ToLowerInvariant();
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
