using System.Threading.Channels;

namespace Honeyslop.Admin.Live;

public sealed class LiveSubscription(ChannelReader<string> reader, Action unsubscribe) : IDisposable
{
    public ChannelReader<string> Reader => reader;

    public void Dispose()
    {
        unsubscribe();
    }
}
