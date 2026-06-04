using System.Text.Json;
using App.Abstractions;
using App.Factory;
using Domain.Machines;
using Infra.RecordStore;

namespace Infra.Queries;

public class PsqlFactoryQueries(ISqlQueryExecutor queryExecutor) : IFactoryQueries
{
    public async Task<FactoryStateSnapshot> GetFactoryStateAsync(CancellationToken ct = default)
    {
        const int PerMachineLimit = 10;

        var machines = (await queryExecutor.QueryManyAsync<MachineRow>(
            """
            SELECT id, alias, sim_speed::float8 AS sim_speed
            FROM machines
            ORDER BY id;
            """,
            ct: ct
        )).ToList();

        var telemetry = (await queryExecutor.QueryManyAsync<TelemetryRow>(
            """
            SELECT machine_id, status, timestamp, part_id, part_status,
                   temperature::float8    AS temperature,
                   vibration::float8      AS vibration,
                   spindle_load::float8   AS spindle_load,
                   cycle_time_sec::float8 AS cycle_time_sec,
                   quality_score::float8  AS quality_score
            FROM (
                SELECT *, ROW_NUMBER() OVER (PARTITION BY machine_id ORDER BY timestamp DESC) AS rn
                FROM machine_telemetry_packets
            ) ranked
            WHERE rn <= @Limit;
            """,
            new { Limit = PerMachineLimit },
            ct
        )).ToList();

        var commands = (await queryExecutor.QueryManyAsync<CommandRow>(
            """
            SELECT id, machine_id, type, created_on, executed_on, parameters::text AS parameters
            FROM (
                SELECT *, ROW_NUMBER() OVER (PARTITION BY machine_id ORDER BY created_on DESC) AS rn
                FROM machine_commands
            ) ranked
            WHERE rn <= @Limit;
            """,
            new { Limit = PerMachineLimit },
            ct
        )).ToList();

        var telemetryByMachine = telemetry
            .GroupBy(t => t.MachineId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var commandsByMachine = commands
            .GroupBy(c => c.MachineId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return new FactoryStateSnapshot(
            machines.Select(m => new FactoryMachine(
                Id: m.Id,
                Alias: m.Alias,
                SimSpeed: m.SimSpeed,
                RecentTelemetry: telemetryByMachine.TryGetValue(m.Id, out var t)
                    ? t.Select(ToTelemetryDto).ToList()
                    : [],
                RecentCommands: commandsByMachine.TryGetValue(m.Id, out var c)
                    ? c.Select(ToCommandDto).ToList()
                    : []
            )).ToList(),
            BuildAvailableCommands()
        );
    }

    private static FactoryTelemetryPacket ToTelemetryDto(TelemetryRow r)
        => new(
            r.MachineId,
            r.Status,
            r.Timestamp,
            r.PartId,
            r.PartStatus,
            r.Temperature,
            r.Vibration,
            r.SpindleLoad,
            r.CycleTimeSec,
            r.QualityScore
        );

    private static FactoryCommand ToCommandDto(CommandRow r)
    {
        var type = (MachineCommandType)r.Type;
        Dictionary<string, string> parameters = string.IsNullOrEmpty(r.Parameters) ? []
            : JsonSerializer.Deserialize<Dictionary<string, string>>(r.Parameters) ?? [];

        return new FactoryCommand(r.Id, r.Type, type.ToDisplayName(), r.CreatedOn, r.ExecutedOn, parameters);
    }

    private static IReadOnlyList<AvailableCommand> BuildAvailableCommands()
        => new List<AvailableCommand>([
            new(MachineCommandType.Resume),
            new(MachineCommandType.Pause),
            new(MachineCommandType.CoolDown),
            new(MachineCommandType.EmergencyStop),
            new(MachineCommandType.InjectSimDefect),
            new(MachineCommandType.AdjustSimSpeed, new List<AvailableCommandField>([
                new("Sim Speed", "sim_speed"),
                new("Spindle Load", "spindle_load")
            ]))
        ]);

    private sealed record MachineRow(string Id, string? Alias, double SimSpeed);

    private sealed record TelemetryRow(
        string MachineId,
        string Status,
        DateTime Timestamp,
        string? PartId,
        string? PartStatus,
        double Temperature,
        double Vibration,
        double SpindleLoad,
        double CycleTimeSec,
        double QualityScore
    );

    private sealed record CommandRow(
        Guid Id,
        string MachineId,
        short Type,
        DateTime CreatedOn,
        DateTime? ExecutedOn,
        string? Parameters
    );
}
