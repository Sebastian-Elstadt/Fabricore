using App.Abstractions;
using App.Parts;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/parts")]
public class PartsController(IPartsQueries partsQueries) : ControllerBase
{
    [HttpGet("{partId}/logs")]
    public Task<IEnumerable<PartLog>> GetLogsAsync([FromRoute] string partId, CancellationToken ct)
        => partsQueries.GetPartLogsAsync(partId, ct);
}