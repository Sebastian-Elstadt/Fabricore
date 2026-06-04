using Domain.Machines;

namespace App.Factory;

public record FactoryStateSnapshot(
    IReadOnlyList<FactoryMachine> Machines,
    IReadOnlyList<AvailableCommand> AvailableCommands,
    IReadOnlyList<LatestPart> LatestParts
);

public record FactoryMachine(
    string Id,
    string? Alias,
    double SimSpeed,
    IReadOnlyList<FactoryTelemetryPacket> RecentTelemetry,
    IReadOnlyList<FactoryCommand> RecentCommands
);

public record FactoryTelemetryPacket(
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

public record FactoryCommand(
    Guid Id,
    short Type,
    string TypeName,
    DateTime CreatedOn,
    DateTime? ExecutedOn,
    IReadOnlyDictionary<string, string> Parameters
);

public record AvailableCommand
{
    public short Id { get; set; }
    public string Name { get; set; }
    public IReadOnlyList<AvailableCommandField>? Fields { get; set; } = null;

    public AvailableCommand(MachineCommandType cmdType, IReadOnlyList<AvailableCommandField>? fields = null)
    {
        Id = (short)cmdType;
        Name = cmdType.ToDisplayName();
        Fields = fields;
    }
}

public record AvailableCommandField(
    string Label,
    string Key
);

public record LatestPart(string Id, DateTime StartedOn, DateTime? FinishedOn);