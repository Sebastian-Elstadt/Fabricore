namespace App.Abstractions;

public interface IRecordStore
{
    IMachineTelemetryPacketRepository MachineTelemetryPacketRepository { get; }
    IMachineCommandRepository MachineCommandRepository { get; }
    IPartRepository PartRepository { get; }
}