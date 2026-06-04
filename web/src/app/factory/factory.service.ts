import { Injectable, OnDestroy, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { API_BASE, CODE_TO_LABEL, normalizeStatus } from './factory.constants';
import {
  AvailableCommand,
  AvailableCommandDto,
  CommandDto,
  CommandEventDto,
  CommandLogEntry,
  ConnectionStatus,
  CreateCommandResponseDto,
  FactoryEventDto,
  FactoryMachineDto,
  FactoryStateSnapshotDto,
  LatestPartDto,
  Machine,
  Part,
  PartFlow,
  PartLog,
  PartLogDto,
  PartProducedEventDto,
  Telemetry,
  TelemetryDto,
  TelemetryEventDto,
} from './factory.models';

const HISTORY_LIMIT = 48;
const COMMAND_LOG_LIMIT = 25;
const PARTS_LIMIT = 40;
/** How long a travelling-part chip animates along a conveyor before it is pruned. */
const FLOW_DURATION_MS = 1500;

/**
 * Owns the client-side mirror of factory state. On {@link connect} it pulls a
 * snapshot from `/api/factory/state`, then keeps it live by subscribing to the
 * `/api/factory/events` SSE stream. All state is exposed as signals so the
 * zoneless UI updates as packets arrive.
 */
@Injectable({ providedIn: 'root' })
export class FactoryService implements OnDestroy {
  private readonly http = inject(HttpClient);

  private readonly _machines = signal<Machine[]>([]);
  private readonly _availableCommands = signal<AvailableCommand[]>([]);
  private readonly _parts = signal<Part[]>([]);
  private readonly _flows = signal<PartFlow[]>([]);
  private readonly _connection = signal<ConnectionStatus>('connecting');
  private readonly _lastError = signal<string | null>(null);

  readonly machines = this._machines.asReadonly();
  readonly availableCommands = this._availableCommands.asReadonly();
  readonly parts = this._parts.asReadonly();
  readonly flows = this._flows.asReadonly();
  readonly connection = this._connection.asReadonly();
  readonly lastError = this._lastError.asReadonly();

  readonly machineCount = computed(() => this._machines().length);

  private eventSource: EventSource | null = null;
  private hasConnected = false;
  private started = false;
  private flowSeq = 0;
  private readonly flowTimers = new Set<ReturnType<typeof setTimeout>>();

  /** Idempotent entry point: fetch the snapshot, then open the live stream. */
  async connect(): Promise<void> {
    if (this.started) return;
    this.started = true;

    await this.loadSnapshot();
    this.openStream();
  }

  ngOnDestroy(): void {
    this.eventSource?.close();
    this.eventSource = null;
    for (const t of this.flowTimers) clearTimeout(t);
    this.flowTimers.clear();
  }

  // --- snapshot ----------------------------------------------------------

  private async loadSnapshot(): Promise<void> {
    try {
      const snapshot = await firstValueFrom(
        this.http.get<FactoryStateSnapshotDto>(`${API_BASE}/api/factory/state`),
      );
      this._machines.set(snapshot.Machines.map(toMachine));
      this._availableCommands.set((snapshot.AvailableCommands ?? []).map(toAvailableCommand));
      this._parts.set((snapshot.LatestParts ?? []).map(toPart).slice(0, PARTS_LIMIT));
      this._lastError.set(null);
    } catch {
      this._lastError.set('Unable to load factory state.');
      this._connection.set('offline');
    }
  }

  // --- live event stream -------------------------------------------------

  private openStream(): void {
    // Guard for non-browser environments (tests/SSR) where EventSource is absent.
    if (typeof EventSource === 'undefined') return;

    this.eventSource?.close();
    this._connection.set(this.hasConnected ? 'reconnecting' : 'connecting');

    const es = new EventSource(`${API_BASE}/api/factory/events`);
    this.eventSource = es;

    es.onopen = () => {
      // A re-open means we may have missed packets while disconnected; resync.
      if (this.hasConnected) void this.loadSnapshot();
      this.hasConnected = true;
      this._connection.set('live');
      this._lastError.set(null);
    };

    es.addEventListener('Telemetry', (e) => {
      const evt = parse<FactoryEventDto>((e as MessageEvent).data);
      if (evt?.Telemetry) this.applyTelemetry(evt.Telemetry);
    });

    es.addEventListener('Command', (e) => {
      const evt = parse<FactoryEventDto>((e as MessageEvent).data);
      if (evt?.Command) this.applyCommand(evt.Command);
    });

    es.addEventListener('PartProduced', (e) => {
      const evt = parse<FactoryEventDto>((e as MessageEvent).data);
      if (evt?.PartProduced) this.applyPartProduced(evt.PartProduced);
    });

    es.onerror = () => {
      // EventSource reconnects on its own; just reflect the gap in the UI.
      this._connection.set('reconnecting');
    };
  }

  private applyTelemetry(evt: TelemetryEventDto): void {
    const telemetry = toTelemetry(evt);
    const idx = this._machines().findIndex((m) => m.id === evt.MachineId);
    const prevPartId = idx >= 0 ? (this._machines()[idx].latest?.partId ?? null) : null;

    this.updateMachine(evt.MachineId, (m) => ({
      ...m,
      latest: telemetry,
      tempHistory: pushCapped(m.tempHistory, telemetry.temperature),
      qualityHistory: pushCapped(m.qualityHistory, telemetry.qualityScore),
    }));

    // A machine reporting a part it wasn't holding a moment ago means the part
    // just arrived — animate it travelling in and track it on the parts panel.
    if (idx >= 0 && telemetry.partId && telemetry.partId !== prevPartId) {
      this.onPartArrived(idx, telemetry.partId, telemetry.timestamp);
    }
  }

  private applyCommand(evt: CommandEventDto): void {
    const entry: CommandLogEntry = {
      id: evt.CommandId,
      typeName: CODE_TO_LABEL[evt.CommandType] ?? evt.CommandType,
      createdOn: evt.Timestamp,
      executedOn: evt.Timestamp,
      parameters: evt.Parameters ?? {},
    };
    this.updateMachine(evt.MachineId, (m) => ({
      ...m,
      commands: upsertCommand(m.commands, entry),
      simSpeed: simSpeedFromParams(evt.Parameters) ?? m.simSpeed,
    }));
  }

  private applyPartProduced(evt: PartProducedEventDto): void {
    const lastIndex = Math.max(0, this._machines().length - 1);
    this.upsertPart({
      id: evt.PartId,
      startedOn: evt.StartedOn,
      finishedOn: evt.FinishedOn,
      lastMachineIndex: lastIndex,
    });
    // Chip leaves the final machine for the finished-goods output.
    this.pushFlow(this._machines().length, evt.PartId, 'output');
  }

  // --- part flow + tracking ---------------------------------------------

  private onPartArrived(machineIndex: number, partId: string, timestamp: string): void {
    const existing = this._parts().find((p) => p.id === partId);
    this.upsertPart({
      id: partId,
      startedOn: existing?.startedOn ?? timestamp,
      finishedOn: existing?.finishedOn ?? null,
      lastMachineIndex: machineIndex,
    });
    // conveyor 0 is the intake before machine 0; otherwise the segment that
    // bridges the upstream machine to this one shares this machine's index.
    this.pushFlow(machineIndex, partId, machineIndex === 0 ? 'intake' : 'transfer');
  }

  private upsertPart(part: Part): void {
    this._parts.update((parts) => {
      const idx = parts.findIndex((p) => p.id === part.id);
      if (idx === -1) return [part, ...parts].slice(0, PARTS_LIMIT);
      const next = parts.slice();
      next[idx] = {
        ...next[idx],
        // Never lose a known finish time or regress a tracked position.
        finishedOn: part.finishedOn ?? next[idx].finishedOn,
        lastMachineIndex: part.lastMachineIndex ?? next[idx].lastMachineIndex,
      };
      return next;
    });
  }

  private pushFlow(conveyorIndex: number, partId: string, kind: PartFlow['kind']): void {
    const id = `flow-${this.flowSeq++}`;
    this._flows.update((flows) => [...flows, { id, conveyorIndex, partId, kind }]);
    const timer = setTimeout(() => {
      this._flows.update((flows) => flows.filter((f) => f.id !== id));
      this.flowTimers.delete(timer);
    }, FLOW_DURATION_MS);
    this.flowTimers.add(timer);
  }

  // --- commands ----------------------------------------------------------

  async sendCommand(
    machineId: string,
    type: number,
    parameters?: Record<string, string>,
  ): Promise<CreateCommandResponseDto> {
    const body = { MachineId: machineId, Type: type, Parameters: parameters ?? null };
    return firstValueFrom(
      this.http.post<CreateCommandResponseDto>(`${API_BASE}/api/commands`, body),
    );
  }

  async getPartLogs(partId: string): Promise<PartLog[]> {
    const logs = await firstValueFrom(
      this.http.get<PartLogDto[]>(`${API_BASE}/api/parts/${encodeURIComponent(partId)}/logs`),
    );
    return (logs ?? []).map(toPartLog);
  }

  // --- helpers -----------------------------------------------------------

  private updateMachine(id: string, project: (m: Machine) => Machine): void {
    this._machines.update((machines) => {
      const idx = machines.findIndex((m) => m.id === id);
      if (idx === -1) return machines;
      const next = machines.slice();
      next[idx] = project(machines[idx]);
      return next;
    });
  }
}

// --- pure mappers --------------------------------------------------------

function toTelemetry(dto: TelemetryDto | TelemetryEventDto): Telemetry {
  return {
    status: normalizeStatus(dto.Status),
    timestamp: dto.Timestamp,
    partId: emptyToNull(dto.PartId),
    partStatus: emptyToNull(dto.PartStatus),
    temperature: dto.Temperature,
    vibration: dto.Vibration,
    spindleLoad: dto.SpindleLoad,
    cycleTimeSec: dto.CycleTimeSec,
    qualityScore: dto.QualityScore,
  };
}

function toCommandEntry(dto: CommandDto): CommandLogEntry {
  return {
    id: dto.Id,
    typeName: dto.TypeName,
    createdOn: dto.CreatedOn,
    executedOn: dto.ExecutedOn,
    parameters: dto.Parameters ?? {},
  };
}

function toAvailableCommand(dto: AvailableCommandDto): AvailableCommand {
  return {
    id: dto.Id,
    name: dto.Name,
    fields: (dto.Fields ?? []).map((f) => ({ label: f.Label, key: f.Key })),
  };
}

function toPart(dto: LatestPartDto): Part {
  return {
    id: dto.Id,
    startedOn: dto.StartedOn,
    finishedOn: emptyToNull(dto.FinishedOn),
    lastMachineIndex: null,
  };
}

function toPartLog(dto: PartLogDto): PartLog {
  return {
    machineId: dto.MachineId,
    timestamp: dto.Timestamp,
    status: normalizeStatus(dto.Status),
    partStatus: emptyToNull(dto.PartStatus),
    temperature: dto.Temperature,
    vibration: dto.Vibration,
    spindleLoad: dto.SpindleLoad,
    cycleTimeSec: dto.CycleTimeSec,
    qualityScore: dto.QualityScore,
  };
}

function toMachine(dto: FactoryMachineDto): Machine {
  // Snapshot lists are newest-first; reverse the telemetry for the sparkline so
  // it reads oldest → newest.
  const telemetry = dto.RecentTelemetry ?? [];
  const latest = telemetry.length ? toTelemetry(telemetry[0]) : null;
  const chrono = telemetry.slice().reverse();
  return {
    id: dto.Id,
    alias: dto.Alias,
    simSpeed: dto.SimSpeed,
    latest,
    commands: (dto.RecentCommands ?? []).map(toCommandEntry),
    tempHistory: chrono.map((t) => t.Temperature),
    qualityHistory: chrono.map((t) => t.QualityScore),
  };
}

function pushCapped(arr: number[], value: number): number[] {
  const next = arr.length >= HISTORY_LIMIT ? arr.slice(1) : arr.slice();
  next.push(value);
  return next;
}

function upsertCommand(log: CommandLogEntry[], entry: CommandLogEntry): CommandLogEntry[] {
  const without = log.filter((c) => c.id !== entry.id);
  return [entry, ...without].slice(0, COMMAND_LOG_LIMIT);
}

function simSpeedFromParams(params: Record<string, string> | undefined): number | null {
  const raw = params?.['sim_speed'];
  if (raw == null) return null;
  const parsed = Number(raw);
  return Number.isFinite(parsed) ? parsed : null;
}

function emptyToNull(value: string | null | undefined): string | null {
  return value == null || value === '' ? null : value;
}

function parse<T>(data: string): T | null {
  try {
    return JSON.parse(data) as T;
  } catch {
    return null;
  }
}
