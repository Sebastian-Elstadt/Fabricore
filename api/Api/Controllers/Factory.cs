using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Realtime;
using App.Abstractions;
using App.Factory;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/factory")]
public class FactoryController(FactoryEventBroadcaster broadcaster, IFactoryQueries queries) : ControllerBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [HttpGet("state")]
    public Task<FactoryStateSnapshot> GetStateAsync(CancellationToken ct)
        => queries.GetFactoryStateAsync(ct);

    [HttpGet("events")]
    public async Task EventsAsync(CancellationToken ct)
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        var (id, reader) = broadcaster.Register();

        // Flush headers + a comment line immediately so subscribers transition to
        // "open" as soon as the stream is established, rather than only once the
        // first event happens to be written.
        await Response.WriteAsync(": connected\n\n", ct);
        await Response.Body.FlushAsync(ct);

        try
        {
            await foreach (var evt in reader.ReadAllAsync(ct))
            {
                var json = JsonSerializer.Serialize(evt, SerializerOptions);
                await Response.WriteAsync($"event: {evt.Type}\n", ct);
                await Response.WriteAsync($"data: {json}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException) { } // ignore disconnects
        finally
        {
            broadcaster.Unregister(id);
        }
    }
}
