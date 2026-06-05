using App.Telemetry;

namespace App.Abstractions;

public interface ITelemetryService {
    Task StoreMachinePacketsAsync(IReadOnlyCollection<StoreMachinePacketCommand> cmds, CancellationToken ct = default);
}