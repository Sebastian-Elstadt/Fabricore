using App.Abstractions;
using Domain.Machines;

namespace App.Commands;

public class CommandsService(IRecordStore recordStore) : ICommandsService
{
    public async Task<MachineCommand> LogMachineCommandAsync(LogMachineCommandCommand cmd, CancellationToken ct = default)
    {
        var machineCommand = cmd.ToMachineCommand();
        await recordStore.MachineCommandRepository.AddAsync(machineCommand, ct);
        return machineCommand;
    }

    public Task MarkCommandExecutedAsync(Guid commandId, CancellationToken ct = default)
    {
        return recordStore.MachineCommandRepository.MarkExecutedAsync(commandId, DateTime.UtcNow, ct);
    }
}