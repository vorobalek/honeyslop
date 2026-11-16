using System.Text;
using Honeyslop.Data.Entities;

namespace Honeyslop.Trap.Responses;

public sealed record CachedResponse(
    long EntryId,
    int Status,
    IReadOnlyList<ResponseHeader> Headers,
    byte[] Body)
{
    public static CachedResponse From(Entry entry)
    {
        return new CachedResponse(
            entry.Id,
            entry.ResponseStatus ?? StatusCodes.Status200OK,
            entry.ResponseHeaders,
            Encoding.UTF8.GetBytes(entry.ResponseBody ?? string.Empty));
    }

    public static CachedResponse From(long entryId, GeneratedResponse response)
    {
        return new CachedResponse(
            entryId,
            response.Status,
            response.Headers,
            Encoding.UTF8.GetBytes(response.Body));
    }
}
