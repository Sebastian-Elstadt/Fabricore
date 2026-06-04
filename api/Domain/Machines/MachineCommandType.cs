namespace Domain.Machines;

public enum MachineCommandType : short
{
    Pause,
    Resume,
    EmergencyStop,
    CoolDown,
    AdjustSimSpeed,
    InjectSimDefect,
    AssignPart
}

public static class MachineCommandTypeName
{
    public const string Pause = "PAUSE";
    public const string Resume = "RESUME";
    public const string EmergencyStop = "EMERGENCY_STOP";
    public const string CoolDown = "COOL_DOWN";
    public const string AdjustSimSpeed = "ADJUST_SPEED";
    public const string InjectSimDefect = "INJECT_DEFECT";
    public const string AssignPart = "ASSIGN_PART";

    public static string ToCode(this MachineCommandType type)
        => type switch
        {
            MachineCommandType.Pause => Pause,
            MachineCommandType.Resume => Resume,
            MachineCommandType.EmergencyStop => EmergencyStop,
            MachineCommandType.CoolDown => CoolDown,
            MachineCommandType.AdjustSimSpeed => AdjustSimSpeed,
            MachineCommandType.InjectSimDefect => InjectSimDefect,
            MachineCommandType.AssignPart => AssignPart,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };

    public static MachineCommandType ToEnum(string name)
        => name switch
        {
            Pause => MachineCommandType.Pause,
            Resume => MachineCommandType.Resume,
            EmergencyStop => MachineCommandType.EmergencyStop,
            CoolDown => MachineCommandType.CoolDown,
            AdjustSimSpeed => MachineCommandType.AdjustSimSpeed,
            InjectSimDefect => MachineCommandType.InjectSimDefect,
            AssignPart => MachineCommandType.AssignPart,
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
        };

    public static string ToDisplayName(this MachineCommandType type)
        => type switch
        {
            MachineCommandType.Pause => "Pause",
            MachineCommandType.Resume => "Resume",
            MachineCommandType.EmergencyStop => "Emergency Stop",
            MachineCommandType.CoolDown => "Cool Down",
            MachineCommandType.AdjustSimSpeed => "Adjust Sim Speed",
            MachineCommandType.InjectSimDefect => "Inject Sim Defect",
            MachineCommandType.AssignPart => "Assign Part",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
}