using App.Abstractions;

namespace App.Commands;

public class CommandsService(IRecordStore recordStore) : ICommandsService
{
    public Task LogMachineCommandAsync(LogMachineCommandCommand cmd, CancellationToken ct = default)
    {
        return recordStore.MachineCommandRepository.AddAsync(cmd.ToMachineCommand(), ct);
    }
}