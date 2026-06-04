namespace App.Parts;

public record PartLog(
    string MachineId,
    DateTime Timestamp,
    string Status,
    string? PartStatus,
    double Temperature,
    double Vibration,
    double SpindleLoad,
    double CycleTimeSec,
    double QualityScore
);