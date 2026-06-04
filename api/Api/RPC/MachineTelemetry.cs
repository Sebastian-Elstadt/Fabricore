using System.Collections.Concurrent;
using System.Threading.Channels;
using Api.Realtime;
using App.Abstractions;
using App.Commands;
using Domain.Machines;
using Grpc.Core;

namespace Api.RPC;

public class MachineTelemetry(
    MachineCommandDispatcher dispatcher,
    FactoryEventBroadcaster broadcaster,
    ITelemetryService telemetryService,
    ICommandsService commandsService,
    IPartsService partsService,
    ILogger<MachineTelemetry> logger
) : Proto.MachineTelemetry.MachineTelemetryBase
{
    private const int TelemetryBatchSize = 5;
    private readonly ConcurrentDictionary<string, DateTime> partHandoffs = [];

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
                DateTime timestamp = DateTimeOffset.FromUnixTimeMilliseconds(msg.TimestampMs).UtcDateTime;

                if (!string.IsNullOrWhiteSpace(msg.PartId))
                {
                    // add if not exists
                    await partsService.TryAddRecordAsync(msg.PartId, timestamp.AddSeconds(msg.CycleTimeSec), ct);
                }

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

                if (msg.CurrentPartStatus == "completed" && !string.IsNullOrWhiteSpace(msg.PartId))
                {
                    await TryHandoffPartAsync(msg.MachineId, msg.PartId, timestamp, ct);
                }

                if (buffer.Count >= TelemetryBatchSize)
                {
                    await StorePacketsAsync(buffer, ct);
                    buffer.Clear();
                }
            }

            // flush any packets left over when the stream ends
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

            if (!Guid.TryParse(cmd.CommandId, out var commandId))
            {
                logger.LogWarning($"Sent command with invalid id '{cmd.CommandId}' to {machineId}; skipping executed-on update.");
                continue;
            }

            await commandsService.MarkCommandExecutedAsync(commandId, ct);
        }
    }

    private string? GetSuccessorMachine(string machineId)
    {
        if (
            machineId.StartsWith("M", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(machineId.AsSpan(1), out var n) &&
            n >= 1 && n < 4
        ) return $"M{n + 1}";
        return null;
    }

    private async Task TryHandoffPartAsync(string fromMachineId, string partId, DateTime messageTimestamp, CancellationToken ct)
    {
        string key = $"{partId}|{fromMachineId}";
        var now = DateTime.UtcNow;

        if (partHandoffs.Count > 2000)
        {
            foreach (var (k, ts) in partHandoffs.ToArray())
            {
                if ((now - ts).TotalMinutes > 60)
                    partHandoffs.TryRemove(k, out _);
            }
        }

        if (!partHandoffs.TryAdd(key, now)) return; // already handed off

        // for now, this is a very simple setup on the server's side.
        // next I want to track the parts here as they move until we hit the last machine,
        // then I want to write an audit trail of the part to the record store.
        var toMachineId = GetSuccessorMachine(fromMachineId);
        if (toMachineId is null)
        {
            var part = await partsService.MarkRecordFinishedAsync(partId, messageTimestamp, ct);

            broadcaster.Broadcast(
                FactoryEvent.ForPartProduced(
                    new FactoryPartProducedEvent(
                        MachineId: fromMachineId,
                        Timestamp: DateTime.UtcNow,
                        PartId: partId,
                        StartedOn: part.StartedOn,
                        FinishedOn: messageTimestamp
                    )
                )
            );

            logger.LogInformation($"Part {partId} completed final stage on {fromMachineId}");
            return;
        }

        logger.LogInformation($"Part handoff: {partId} completed on {fromMachineId} → dispatching ASSIGN_PART to {toMachineId}");

        try
        {
            var cmd = await commandsService.LogMachineCommandAsync(
                new LogMachineCommandCommand(
                    toMachineId,
                    MachineCommandType.AssignPart,
                    new Dictionary<string, string> { ["part_id"] = partId }
                ),
                ct
            );

            var message = new Proto.CommandMessage
            {
                CommandId = cmd.Id.ToString(),
                CommandType = cmd.Type.ToCode(),
                TimestampMs = new DateTimeOffset(cmd.CreatedOn, TimeSpan.Zero).ToUnixTimeMilliseconds()
            };

            message.Parameters.Add("part_id", partId);

            var dispatched = dispatcher.TryDispatch(toMachineId, message);
            if (!dispatched)
                logger.LogWarning($"Handoff ASSIGN_PART for {partId} to {toMachineId} was logged but {toMachineId} is not currently connected.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to perform part handoff for {partId} from {fromMachineId}");
            partHandoffs.TryRemove(key, out _);
        }
    }
}
