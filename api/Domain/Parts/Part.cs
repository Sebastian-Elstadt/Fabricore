namespace Domain.Parts;

public class Part
{
    public string Id { get; init; }
    public DateTime StartedOn { get; init; }

    private DateTime? _finishedOn = null;
    public DateTime? FinishedOn
    {
        get => _finishedOn;
        set
        {
            if(value.HasValue && value.Value < StartedOn)
                throw new ArgumentException("Finished on must be after the started on");
            _finishedOn = value;
        }
    }

    private Part() { }
    public Part(string id, DateTime startedOn)
    {
        Id = id;
        StartedOn = startedOn;
    }

    public static Part Reconstitute(
        string id,
        DateTime startedOn,
        DateTime? finishedOn
    ) => new Part
    {
        Id = id,
        StartedOn = startedOn,
        _finishedOn = finishedOn
    };
}