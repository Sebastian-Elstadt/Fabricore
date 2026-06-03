namespace Domain.Telemetry;

public class MachineTelemetryPacket
{
    public string MachineId { get; init; }
    public string Status { get; init; }
    public DateTime Timestamp { get; init; }

    public string? PartId { get; init; }
    public string? PartStatus { get; init; }

    public double Temperature { get; init; }
    public double Vibration { get; init; }
    public double SpindleLoad { get; init; }
    public double CycleTimeSec { get; init; }
    public double QualityScore { get; init; }

    public MachineTelemetryPacket(string machineId, string status, DateTime timestamp)
    {
        MachineId = machineId;
        Status = status;
        Timestamp = timestamp;
    }
}