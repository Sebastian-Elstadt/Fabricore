using App.Abstractions;

namespace App.Telemetry;

public class TelemetryService(IRecordStore recordStore) : ITelemetryService
{
    public Task StoreMachinePacketsAsync(IReadOnlyCollection<StoreMachinePacketCommand> cmds, CancellationToken ct = default)
    {
        var packets = cmds.Select(c => c.ToMachineTelemetryPacket()).ToList();
        return recordStore.MachineTelemetryPacketRepository.AddRangeAsync(packets, ct);
    }
}