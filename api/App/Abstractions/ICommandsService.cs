using App.Commands;
using Domain.Machines;

namespace App.Abstractions;

public interface ICommandsService {
    Task<MachineCommand> LogMachineCommandAsync(LogMachineCommandCommand cmd, CancellationToken ct = default);
    Task MarkCommandExecutedAsync(Guid commandId, CancellationToken ct = default);
}