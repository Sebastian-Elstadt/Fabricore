namespace App.Abstractions;

public interface IRecordStore
{
    IMachineTelemetryPacketRepository MachineTelemetryPacketRepository { get; }
}