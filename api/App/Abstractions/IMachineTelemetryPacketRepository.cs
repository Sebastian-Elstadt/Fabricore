using Domain.Telemetry;

namespace App.Abstractions;

public interface IMachineTelemetryPacketRepository {
    Task AddAsync(MachineTelemetryPacket packet, CancellationToken ct = default);
}