using System.Diagnostics.CodeAnalysis;
using Honeyslop.Data.Entities;

namespace Honeyslop.Trap.Requests;

public interface IRequestLog
{
    void Add(RequestRecord record);
    void Update(RequestRecord record);
    IAsyncEnumerable<RequestLogItem> ReadAllAsync(CancellationToken cancellationToken);
    bool TryRead([MaybeNullWhen(false)] out RequestLogItem item);
}
