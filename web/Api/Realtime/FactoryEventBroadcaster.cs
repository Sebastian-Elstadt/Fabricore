using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Api.Realtime;

public class FactoryEventBroadcaster
{
    private readonly ConcurrentDictionary<Guid, Channel<FactoryEvent>> subscribers = new();

    public (Guid Id, ChannelReader<FactoryEvent> Reader) Register()
    {
        var channel = Channel.CreateBounded<FactoryEvent>(
            new BoundedChannelOptions(256)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            }
        );

        var id = Guid.NewGuid();
        subscribers[id] = channel;

        return (id, channel.Reader);
    }

    public void Unregister(Guid id)
    {
        if (!subscribers.TryRemove(id, out var channel)) return;
        channel.Writer.TryComplete();
    }

    public void Broadcast(FactoryEvent evt)
    {
        foreach (var channel in subscribers.Values)
            channel.Writer.TryWrite(evt);
    }
}
