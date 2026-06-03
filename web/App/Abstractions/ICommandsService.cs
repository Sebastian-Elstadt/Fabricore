using App.Commands;

namespace App.Abstractions;

public interface ICommandsService {
    Task LogMachineCommandAsync(LogMachineCommandCommand cmd, CancellationToken ct = default);
}