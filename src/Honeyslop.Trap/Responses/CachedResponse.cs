using System.Text;
using Honeyslop.Data.Entities;
using Honeyslop.Data.Enums;

namespace Honeyslop.Trap.Responses;

public sealed record CachedResponse(
    long EntryId,
    int Status,
    IReadOnlyList<ResponseHeader> Headers,
    byte[] Body,
    TimeSpan? Delay = null,
    NoiseTier? Noise = null)
{
    public static CachedResponse From(Entry entry)
    {
        return new CachedResponse(
            entry.Id,
            entry.ResponseStatus ?? StatusCodes.Status200OK,
            entry.ResponseHeaders,
            Encoding.UTF8.GetBytes(entry.ResponseBody ?? string.Empty),
            DelayOf(entry),
            entry.Noise);
    }

    public static TimeSpan? DelayOf(Entry entry)
    {
        return entry.ResponseDelayMilliseconds is { } milliseconds ? TimeSpan.FromMilliseconds(milliseconds) : null;
    }
}