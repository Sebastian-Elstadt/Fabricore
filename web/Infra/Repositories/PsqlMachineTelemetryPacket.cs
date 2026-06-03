using App.Abstractions;
using Domain.Telemetry;

namespace Infra.Repositories;

public class PsqlMachineTelemetryPacketRepository : IMachineTelemetryPacketRepository
{
    public Task AddAsync(MachineTelemetryPacket packet, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}