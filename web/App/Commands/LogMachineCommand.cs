using Domain.Machines;

namespace App.Commands;

public record LogMachineCommandCommand(
    string MachineId,
    MachineCommandType Type
)
{
    public MachineCommand ToMachineCommand()
        => new MachineCommand(MachineId, Type);
}