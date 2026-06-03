using Domain.Machines;

namespace App.Abstractions;

public interface IMachineCommandRepository
{
    Task AddAsync(MachineCommand cmd, CancellationToken ct = default);
}