namespace App.Factory;

public record FactoryStateSnapshot(
    IReadOnlyList<FactoryMachine> Machines,
    IReadOnlyList<AvailableCommand> AvailableCommands
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

public record AvailableCommand(
    short Id,
    string Name
);
