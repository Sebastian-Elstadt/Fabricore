using System.Text;
using App.Abstractions;
using Dapper;
using Domain.Telemetry;
using Infra.RecordStore;

namespace Infra.Repositories;

public class PsqlMachineTelemetryPacketRepository(ISqlQueryExecutor executor) : IMachineTelemetryPacketRepository
{
    public Task AddRangeAsync(IReadOnlyCollection<MachineTelemetryPacket> packets, CancellationToken ct = default)
    {
        if (packets.Count == 0) return Task.CompletedTask;

        var parameters = new DynamicParameters();
        var values = new StringBuilder(
            """
            INSERT INTO machine_telemetry_packets (
                machine_id, status, timestamp, part_id, part_status,
                temperature, vibration, spindle_load, cycle_time_sec, quality_score
            )
            VALUES
            """
        );

        var i = 0;
        foreach (var p in packets)
        {
            if (i > 0) values.Append(',');
            values.Append($"""
                (@MachineId{i}, @Status{i}, @Timestamp{i}, @PartId{i}::TEXT, @PartStatus{i}::TEXT,
                 @Temperature{i}, @Vibration{i}, @SpindleLoad{i}, @CycleTimeSec{i}, @QualityScore{i})
                """);

            parameters.Add($"MachineId{i}", p.MachineId);
            parameters.Add($"Status{i}", p.Status);
            parameters.Add($"Timestamp{i}", p.Timestamp);
            parameters.Add($"PartId{i}", p.PartId);
            parameters.Add($"PartStatus{i}", p.PartStatus);
            parameters.Add($"Temperature{i}", p.Temperature);
            parameters.Add($"Vibration{i}", p.Vibration);
            parameters.Add($"SpindleLoad{i}", p.SpindleLoad);
            parameters.Add($"CycleTimeSec{i}", p.CycleTimeSec);
            parameters.Add($"QualityScore{i}", p.QualityScore);
            i++;
        }

        values.Append(';');
        return executor.ExecuteAsync(values.ToString(), parameters, ct);
    }
}
