using System.Text.Json;
using Grpc.Core;

namespace Api.RPC;

public class MachineTelemetry(ILogger<MachineTelemetry> logger) : Proto.MachineTelemetry.MachineTelemetryBase
{
    public override async Task TelemetryStream(
        IAsyncStreamReader<Proto.TelemetryMessage> readStream,
        IServerStreamWriter<Proto.CommandMessage> writeStream,
        ServerCallContext ctx
    )
    {
        logger.LogInformation($"Machine connected: {ctx.Peer}");

        var ct = ctx.CancellationToken;

        // Todo: handle writing to stream
        await ReadIncomingAsync(readStream, ct);

        logger.LogInformation($"Machine disconnected: {ctx.Peer}");
    }

    private async Task ReadIncomingAsync(IAsyncStreamReader<Proto.TelemetryMessage> readStream, CancellationToken ct = default)
    {
        try
        {
            await foreach (var msg in readStream.ReadAllAsync(ct))
            {
                logger.LogInformation("Received telemetry message:\n" + JsonSerializer.Serialize(msg));
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Machine read stream error: {ex}");
        }
    }
}