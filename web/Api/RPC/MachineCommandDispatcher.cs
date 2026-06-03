using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Api.RPC;

public class MachineCommandDispatcher
{
    private readonly ConcurrentDictionary<string, Channel<Proto.CommandMessage>> channels = new();

    public ChannelReader<Proto.CommandMessage> Register(string machineId)
    {
        var channel = Channel.CreateBounded<Proto.CommandMessage>(
            new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true
            }
        );

        Unregister(machineId);
        channels[machineId] = channel;
        return channel;
    }

    public void Unregister(string machineId)
    {
        if (!channels.TryGetValue(machineId, out var channel)) return;
        channel.Writer.TryComplete();
    }

    public bool TryDispatch(string machineId, Proto.CommandMessage cmd) {
        return channels.TryGetValue(machineId, out var channel)
            && channel.Writer.TryWrite(cmd);
    }
}