using App.Abstractions;
using Infra.Repositories;

namespace Infra.RecordStore;

public class PsqlRecordStore : IRecordStore
{
    private IMachineTelemetryPacketRepository? _machineTelemetryPacketRepository;
    public IMachineTelemetryPacketRepository MachineTelemetryPacketRepository => _machineTelemetryPacketRepository ??= new PsqlMachineTelemetryPacketRepository();
}