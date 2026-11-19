using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Honeyslop.Data.Entities;
using Honeyslop.Trap.Monitoring;

namespace Honeyslop.Trap.Requests.Internals;

internal sealed class RequestLog : IRequestLog
{
    private const int Capacity = 20000;

    private readonly Channel<RequestLogItem> _channel = Channel.CreateBounded<RequestLogItem>(
        new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true
        },
        _ => TrapMetrics.RequestLogDropped.Inc());

    public void Add(RequestRecord record)
    {
        _channel.Writer.TryWrite(new RequestLogItem(record, false));
    }

    public void Update(RequestRecord record)
    {
        _channel.Writer.TryWrite(new RequestLogItem(record, true));
    }

    public IAsyncEnumerable<RequestLogItem> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public bool TryRead([MaybeNullWhen(false)] out RequestLogItem item)
    {
        return _channel.Reader.TryRead(out item);
    }
}