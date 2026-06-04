using App.Abstractions;
using Infra.Repositories;

namespace Infra.RecordStore.Psql;

public class PsqlRecordStore : PsqlQueryExecutor, IRecordStore
{
    public PsqlRecordStore(RecordStoreConfig config) : base(config.ConnectionString) { }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (transaction is not null)
            throw new InvalidOperationException("Transaction is already in progress.");

        transaction = await connection.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (transaction is null)
            throw new InvalidOperationException("No transaction is in progress.");

        await transaction.CommitAsync(ct);
        transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (transaction is null)
            throw new InvalidOperationException("No transaction is in progress.");

        await transaction.RollbackAsync(ct);
        transaction = null;
    }

    private IMachineTelemetryPacketRepository? _machineTelemetryPacketRepository;
    public IMachineTelemetryPacketRepository MachineTelemetryPacketRepository
        => _machineTelemetryPacketRepository ??= new PsqlMachineTelemetryPacketRepository(this);

    private IMachineCommandRepository? _machineCommandRepository;
    public IMachineCommandRepository MachineCommandRepository
        => _machineCommandRepository ??= new PsqlMachineCommandRepository(this);
}