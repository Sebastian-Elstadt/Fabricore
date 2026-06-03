namespace Domain.Machines;

public enum MachineCommandType
{
    Pause,
    Resume,
    EmergencyStop,
    CoolDown,
    AdjustSimSpeed,
    InjectSimDefect
}

public static class MachineCommandTypeName
{
    public const string Pause = "PAUSE";
    public const string Resume = "RESUME";
    public const string EmergencyStop = "EMERGENCY_STOP";
    public const string CoolDown = "COOL_DOWN";
    public const string AdjustSimSpeed = "ADJUST_SPEED";
    public const string InjectSimDefect = "INJECT_DEFECT";

    public static string ToName(this MachineCommandType type)
        => type switch
        {
            MachineCommandType.Pause => Pause,
            MachineCommandType.Resume => Resume,
            MachineCommandType.EmergencyStop => EmergencyStop,
            MachineCommandType.CoolDown => CoolDown,
            MachineCommandType.AdjustSimSpeed => AdjustSimSpeed,
            MachineCommandType.InjectSimDefect => InjectSimDefect,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
}