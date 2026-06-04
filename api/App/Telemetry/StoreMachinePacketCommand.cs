using Domain.Telemetry;

namespace App.Telemetry;

public record StoreMachinePacketCommand(
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
)
{
    public MachineTelemetryPacket ToMachineTelemetryPacket()
        => new MachineTelemetryPacket(MachineId, Status, Timestamp)
        {
            PartId = PartId,
            PartStatus = PartStatus,
            Temperature = Temperature,
            Vibration = Vibration,
            SpindleLoad = SpindleLoad,
            CycleTimeSec = CycleTimeSec,
            QualityScore = QualityScore,
        };
}