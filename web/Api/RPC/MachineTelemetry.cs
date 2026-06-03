using System.Text.Json;
using App.Abstractions;
using Grpc.Core;

namespace Api.RPC;

public class MachineTelemetry(
    ITelemetryService telemetryService,
    ILogger<MachineTelemetry> logger
) : Proto.MachineTelemetry.MachineTelemetryBase
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
            // A big note here:
            // 'telemetryService' is injected up top, meaning it will keep resources active for the entire duration of this connection.
            // Specifically an sql database connection.
            // Another approach is to spawn a new scoped instance inside this foreach loop to only create* a connection per incoming telemetry message.
            // However, these messages come in very frequently. So I think it's better to keep 'telemetryService' alive across the entire connection that will write a bunch
            // of times rather than spawning a new instance and establishing a new connection every 5 seconds (or however long the telemetry interval is).

            // Another approach would be to buffer the incoming packets and only instantiate the 'telemetryService' for say every 10 packets, then dump them all to the database.
            // But I want live data on the GUI. But! I could also do the buffered persistence, and then just immediately broadcast incoming packets to the GUI over some websocket/tcp connection.
            // Will get back to this.

            // * I understand it's not _really_ closing and reopening connections, the psql server will manage connections in its pool however it wants. But the principle remains, don't keep resources you don't need.

            await foreach (var msg in readStream.ReadAllAsync(ct))
            {
                logger.LogInformation("Received telemetry message:\n" + JsonSerializer.Serialize(msg));
                await telemetryService.StoreMachinePacketAsync(new App.Telemetry.StoreMachinePacketCommand(
                    MachineId: msg.MachineId,
                    Status: msg.Status,
                    Timestamp: DateTimeOffset.FromUnixTimeMilliseconds(msg.TimestampMs).UtcDateTime,
                    PartId: msg.PartId,
                    PartStatus: msg.CurrentPartStatus,
                    Temperature: msg.Temperature,
                    Vibration: msg.Vibration,
                    SpindleLoad: msg.SpindleLoad,
                    CycleTimeSec: msg.CycleTimeSec,
                    QualityScore: msg.QualityScore
                ), ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Machine read stream error: {ex}");
        }
    }
}