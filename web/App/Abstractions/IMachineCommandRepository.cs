using Domain.Machines;

namespace App.Abstractions;

public interface IMachineCommandRepository
{
    Task AddAsync(MachineCommand cmd, CancellationToken ct = default);
    Task MarkExecutedAsync(Guid id, DateTime executedOn, CancellationToken ct = default);
}