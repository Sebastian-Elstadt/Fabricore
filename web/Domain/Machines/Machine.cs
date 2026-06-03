namespace Domain.Machines;

public class Machine
{
    public string Id { get; init; } = string.Empty;

    private string? _alias;
    public string? Alias
    {
        get => _alias;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _alias = null;
                return;
            }

            string trimmed = value.Trim();
            if (trimmed.Length > 100)
                throw new ArgumentException("Alias must be 100 characters or less");

            _alias = trimmed;
        }
    }

    public double SimSpeed { get; set; } = 1.0;
    public uint PartCycleBaseMs { get; set; } = 8000;
    public uint TelemetryIntervalMs { get; set; } = 3000;

    private Machine() { }
    public Machine(string id, string? alias = null)
    {
        Id = id;
        Alias = alias;
    }

    public static Machine Reconstitute(
        string id,
        string? alias,
        double simSpeed,
        uint partCycleBaseMs,
        uint telemetryIntervalMs
    ) => new Machine
    {
        Id = id,
        _alias = alias,
        SimSpeed = simSpeed,
        PartCycleBaseMs = partCycleBaseMs,
        TelemetryIntervalMs = telemetryIntervalMs
    };
}