using App.Abstractions;
using Infra.Repositories;

namespace Infra.RecordStore.Psql;

public class PsqlRecordStore : PsqlQueryExecutor, IRecordStore
{
    public PsqlRecordStore(RecordStoreConfig config) : base(config.ConnectionString) { }

    private IMachineTelemetryPacketRepository? _machineTelemetryPacketRepository;
    public IMachineTelemetryPacketRepository MachineTelemetryPacketRepository
        => _machineTelemetryPacketRepository ??= new PsqlMachineTelemetryPacketRepository(this);

    private IMachineCommandRepository? _machineCommandRepository;
    public IMachineCommandRepository MachineCommandRepository
        => _machineCommandRepository ??= new PsqlMachineCommandRepository(this);

    private IPartRepository? _partRepository;
    public IPartRepository PartRepository
        => _partRepository ??= new PsqlPartRepository(this);
}