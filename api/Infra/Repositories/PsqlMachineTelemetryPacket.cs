using App.Abstractions;
using Domain.Telemetry;
using Infra.RecordStore;

namespace Infra.Repositories;

public class PsqlMachineTelemetryPacketRepository(ISqlQueryExecutor executor) : IMachineTelemetryPacketRepository
{
    public Task AddAsync(MachineTelemetryPacket packet, CancellationToken ct = default)
    {
        return executor.ExecuteAsync(
            """
            INSERT INTO machine_telemetry_packets (
                machine_id,
                status,
                timestamp,
                part_id,
                part_status,
                temperature,
                vibration,
                spindle_load,
                cycle_time_sec,
                quality_score
            )
            VALUES (
                @MachineId,
                @Status,
                @Timestamp,
                @PartId::TEXT,
                @PartStatus::TEXT,
                @Temperature,
                @Vibration,
                @SpindleLoad,
                @CycleTimeSec,
                @QualityScore
            );
            """,
            new {
                packet.MachineId,
                packet.Status,
                packet.Timestamp,
                packet.PartId,
                packet.PartStatus,
                packet.Temperature,
                packet.Vibration,
                packet.SpindleLoad,
                packet.CycleTimeSec,
                packet.QualityScore
            },
            ct
        );
    }
}