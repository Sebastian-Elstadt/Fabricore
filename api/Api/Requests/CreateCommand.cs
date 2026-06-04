using Domain.Machines;

namespace Api.Requests;

public record CreateCommandRequest(
    string MachineId,
    MachineCommandType Type,
    IReadOnlyDictionary<string, string>? Parameters
);