using Domain.Machines;

namespace App.Commands;

public record LogMachineCommandCommand(
    string MachineId,
    MachineCommandType Type,
    IReadOnlyDictionary<string, string>? Parameters = null
)
{
    public MachineCommand ToMachineCommand()
        => new MachineCommand(MachineId, Type, Parameters);
}