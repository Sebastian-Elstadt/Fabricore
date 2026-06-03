using System.Text.Json;
using System.Threading.Channels;
using Api.Realtime;
using App.Abstractions;
using Domain.Machines;
using Grpc.Core;

namespace Api.RPC;

public class MachineTelemetry(
    MachineCommandDispatcher dispatcher,
    FactoryEventBroadcaster broadcaster,
    IServiceScopeFactory scopeFactory,
    ILogger<MachineTelemetry> logger
) : Proto.MachineTelemetry.MachineTelemetryBase
{
    private const int TelemetryBatchSize = 5;

    public override async Task TelemetryStream(
        IAsyncStreamReader<Proto.TelemetryMessage> readStream,
        IServerStreamWriter<Proto.CommandMessage> writeStream,
        ServerCallContext ctx
    )
    {
        var ct = ctx.CancellationToken;
        string? machineId = ctx.RequestHeaders.GetValue("machine-id");
        if (string.IsNullOrWhiteSpace(machineId))
        {
            logger.LogWarning($"Machine ID not provided on connection: {ctx.Peer}");
            return;
        }

        logger.LogInformation($"Machine connected: {machineId} | {ctx.Peer}");
        var reader = dispatcher.Register(machineId);

        try
        {
            await Task.WhenAll(
                ReadIncomingAsync(readStream, ct),
                WriteOutgoingAsync(machineId, reader, writeStream, ct)
            );
        }
        finally
        {
            dispatcher.Unregister(machineId);
            logger.LogInformation($"Machine disconnected: {ctx.Peer}");
        }
    }

    private async Task ReadIncomingAsync(IAsyncStreamReader<Proto.TelemetryMessage> readStream, CancellationToken ct)
    {
        var buffer = new List<App.Telemetry.StoreMachinePacketCommand>(TelemetryBatchSize);

        try
        {
            await foreach (var msg in readStream.ReadAllAsync(ct))
            {
                logger.LogInformation("Received telemetry message:\n" + JsonSerializer.Serialize(msg));
                DateTime timestamp = DateTimeOffset.FromUnixTimeMilliseconds(msg.TimestampMs).UtcDateTime;

                buffer.Add(new App.Telemetry.StoreMachinePacketCommand(
                    MachineId: msg.MachineId,
                    Status: msg.Status,
                    Timestamp: timestamp,
                    PartId: msg.PartId,
                    PartStatus: msg.CurrentPartStatus,
                    Temperature: msg.Temperature,
                    Vibration: msg.Vibration,
                    SpindleLoad: msg.SpindleLoad,
                    CycleTimeSec: msg.CycleTimeSec,
                    QualityScore: msg.QualityScore
                ));

                broadcaster.Broadcast(
                    FactoryEvent.ForTelemetry(
                        new FactoryTelemetryEvent(
                            MachineId: msg.MachineId,
                            Status: msg.Status,
                            Timestamp: timestamp,
                            PartId: msg.PartId,
                            PartStatus: msg.CurrentPartStatus,
                            Temperature: msg.Temperature,
                            Vibration: msg.Vibration,
                            SpindleLoad: msg.SpindleLoad,
                            CycleTimeSec: msg.CycleTimeSec,
                            QualityScore: msg.QualityScore
                        )
                    )
                );

                if (buffer.Count >= TelemetryBatchSize)
                {
                    await StorePacketsAsync(buffer, ct);
                    buffer.Clear();
                }
            }

            // Flush any packets left over when the stream ends.
            if (buffer.Count > 0)
                await StorePacketsAsync(buffer, ct);
        }
        catch (Exception ex)
        {
            logger.LogError($"Machine read stream error: {ex}");
        }
    }

    private async Task StorePacketsAsync(IReadOnlyList<App.Telemetry.StoreMachinePacketCommand> packets, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var telemetryService = scope.ServiceProvider.GetRequiredService<ITelemetryService>();

        foreach (var packet in packets)
            await telemetryService.StoreMachinePacketAsync(packet, ct);
    }

    private async Task WriteOutgoingAsync(string machineId, ChannelReader<Proto.CommandMessage> reader, IServerStreamWriter<Proto.CommandMessage> writeStream, CancellationToken ct)
    {
        await foreach (var cmd in reader.ReadAllAsync(ct))
        {
            await writeStream.WriteAsync(cmd);

            broadcaster.Broadcast(
                FactoryEvent.ForCommand(
                    new FactoryCommandEvent(
                        MachineId: machineId,
                        CommandId: cmd.CommandId,
                        CommandType: cmd.CommandType,
                        Parameters: new Dictionary<string, string>(cmd.Parameters),
                        Timestamp: DateTime.UtcNow
                    )
                )
            );

            await using var scope = scopeFactory.CreateAsyncScope();
            var commandsService = scope.ServiceProvider.GetRequiredService<ICommandsService>();
            await commandsService.LogMachineCommandAsync(new(machineId, MachineCommandTypeName.ToEnum(cmd.CommandType)));
        }
    }
}
