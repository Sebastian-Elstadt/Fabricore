using System.Text.Json.Serialization;

namespace Api.Realtime;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FactoryEventType
{
    Telemetry,
    Command
}

public record FactoryEvent(
    FactoryEventType Type,
    string MachineId,
    DateTime Timestamp,
    FactoryTelemetryEvent? Telemetry = null,
    FactoryCommandEvent? Command = null
)
{
    public static FactoryEvent ForTelemetry(FactoryTelemetryEvent ev)
        => new(FactoryEventType.Telemetry, ev.MachineId, ev.Timestamp, Telemetry: ev);

    public static FactoryEvent ForCommand(FactoryCommandEvent ev)
        => new(FactoryEventType.Command, ev.MachineId, ev.Timestamp, Command: ev);
}

public record FactoryTelemetryEvent(
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

public record FactoryCommandEvent(
    string MachineId,
    string CommandId,
    string CommandType,
    IReadOnlyDictionary<string, string> Parameters,
    DateTime Timestamp
);
