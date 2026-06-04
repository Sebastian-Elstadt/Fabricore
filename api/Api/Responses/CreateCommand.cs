namespace Api.Responses;

public record CreateCommandResponse(
    Guid CommandId,
    bool Dispatched
);