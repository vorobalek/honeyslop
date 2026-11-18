using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;

namespace Honeyslop.Admin.Core.Extensions;

public static class EntryExtensions
{
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

        public int BodyLength => entry.ResponseBody?.Length ?? 0;

        public string? ContentType => entry.ResponseHeaders
            .FirstOrDefault(x => x.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))?
            .Value;

        public bool CanPreview =>
            !string.IsNullOrEmpty(entry.ResponseBody) &&
            entry.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true;
    }
}
