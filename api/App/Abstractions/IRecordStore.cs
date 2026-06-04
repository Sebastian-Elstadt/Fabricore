namespace App.Abstractions;

public interface IRecordStore
{
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);

    IMachineTelemetryPacketRepository MachineTelemetryPacketRepository { get; }
    IMachineCommandRepository MachineCommandRepository { get; }
}