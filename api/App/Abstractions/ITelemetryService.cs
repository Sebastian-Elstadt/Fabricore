using App.Telemetry;

namespace App.Abstractions;

public interface ITelemetryService {
    Task StoreMachinePacketAsync(StoreMachinePacketCommand cmd, CancellationToken ct = default);
}