namespace Domain.Machines;

public class MachineCommand
{
    public Guid Id { get; private init; } = Guid.CreateVersion7();
    public DateTime CreatedOn { get; private init; } = DateTime.UtcNow;
    public DateTime? ExecutedOn { get; private set; }

    public string MachineId { get; init; } = string.Empty;
    public MachineCommandType Type { get; init; }
    public IReadOnlyDictionary<string, string> Parameters { get; private init; } = new Dictionary<string, string>();

    public void MarkExecuted(DateTime executedOn) => ExecutedOn = executedOn;

    private MachineCommand() { }
    public MachineCommand(string machineId, MachineCommandType type, IReadOnlyDictionary<string, string>? parameters = null)
    {
        MachineId = machineId;
        Type = type;
        Parameters = parameters ?? new Dictionary<string, string>();
    }

    public static MachineCommand Reconstitute(
        Guid id,
        DateTime createdOn,
        string machineId,
        MachineCommandType type,
        DateTime? executedOn,
        IReadOnlyDictionary<string, string> parameters
    ) => new MachineCommand
    {
        Id = id,
        CreatedOn = createdOn,
        MachineId = machineId,
        Type = type,
        ExecutedOn = executedOn,
        Parameters = parameters
    };
}