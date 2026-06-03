using App.Abstractions;

namespace App.Telemetry;

public class TelemetryService(IRecordStore recordStore) : ITelemetryService
{
    public async Task StoreMachinePacketAsync(StoreMachinePacketCommand cmd, CancellationToken ct = default)
    {
        var packet = cmd.ToMachineTelemetryPacket();
        await recordStore.MachineTelemetryPacketRepository.AddAsync(packet, ct);
    }
}