using Domain.Telemetry;

namespace App.Abstractions;

public interface IMachineTelemetryPacketRepository {
    Task AddRangeAsync(IReadOnlyCollection<MachineTelemetryPacket> packets, CancellationToken ct = default);
}