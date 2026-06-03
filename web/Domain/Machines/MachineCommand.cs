namespace Domain.Machines;

public class MachineCommand
{
    public Guid Id { get; private init; } = Guid.CreateVersion7();
    public DateTime CreatedOn { get; private init; } = DateTime.UtcNow;

    public string MachineId { get; init; } = string.Empty;
    public MachineCommandType Type { get; init; }

    private MachineCommand() { }
    public MachineCommand(string machineId, MachineCommandType type)
    {
        MachineId = machineId;
        Type = type;
    }

    public static MachineCommand Reconstitute(
        Guid id,
        DateTime createdOn,
        string machineId,
        MachineCommandType type
    ) => new MachineCommand
    {
        Id = id,
        CreatedOn = createdOn,
        MachineId = machineId,
        Type = type
    };
}