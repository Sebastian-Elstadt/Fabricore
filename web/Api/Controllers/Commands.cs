using Api.Requests;
using Api.Responses;
using Api.RPC;
using App.Abstractions;
using App.Commands;
using Domain.Machines;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("commands")]
public class CommandsController(
    ICommandsService commandsService,
    MachineCommandDispatcher dispatcher,
    ILogger<CommandsController> logger
) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateCommandResponse>> CreateCommandAsync(
        [FromBody] CreateCommandRequest request,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(request.MachineId))
            return BadRequest("MachineId is required.");

        if (!Enum.IsDefined(request.Type))
            return BadRequest($"Unknown command type '{request.Type}'.");

        var cmd = await commandsService.LogMachineCommandAsync(
            new LogMachineCommandCommand(request.MachineId, request.Type, request.Parameters),
            ct
        );

        var message = new Proto.CommandMessage
        {
            CommandId = cmd.Id.ToString(),
            CommandType = cmd.Type.ToCode(),
            TimestampMs = new DateTimeOffset(cmd.CreatedOn, TimeSpan.Zero).ToUnixTimeMilliseconds()
        };

        foreach (var (key, value) in cmd.Parameters) message.Parameters.Add(key, value);

        var dispatched = dispatcher.TryDispatch(request.MachineId, message);
        if (!dispatched)
            logger.LogWarning($"Command {cmd.Id} persisted but machine {request.MachineId} is not connected; not delivered.");

        return Ok(new CreateCommandResponse(cmd.Id, dispatched));
    }
}
