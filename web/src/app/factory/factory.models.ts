/**
 * Wire models mirror the JSON shapes returned by the API exactly (PascalCase,
 * `PropertyNamingPolicy = null` on the server). They are mapped into the
 * camelCase view-models below before reaching the UI.
 */

export interface FactoryStateSnapshotDto {
  Machines: FactoryMachineDto[];
  AvailableCommands: AvailableCommandDto[];
  LatestParts: LatestPartDto[];
}

export interface LatestPartDto {
  Id: string;
  StartedOn: string;
  FinishedOn: string | null;
}

export interface FactoryMachineDto {
  Id: string;
  Alias: string | null;
  SimSpeed: number;
  RecentTelemetry: TelemetryDto[];
  RecentCommands: CommandDto[];
}

export interface TelemetryDto {
  MachineId: string;
  Status: string;
  Timestamp: string;
  PartId: string | null;
  PartStatus: string | null;
  Temperature: number;
  Vibration: number;
  SpindleLoad: number;
  CycleTimeSec: number;
  QualityScore: number;
}

export interface CommandDto {
  Id: string;
  Type: number;
  TypeName: string;
  CreatedOn: string;
  ExecutedOn: string | null;
  Parameters: Record<string, string>;
}

export interface AvailableCommandDto {
  Id: number;
  Name: string;
  /** Extra parameters the operator must supply for this command, when present. */
  Fields?: AvailableCommandFieldDto[] | null;
}

export interface AvailableCommandFieldDto {
  Label: string;
  Key: string;
}

/** Per-machine telemetry row returned by `/api/parts/{id}/logs`. */
export interface PartLogDto {
  MachineId: string;
  Timestamp: string;
  Status: string;
  PartStatus: string | null;
  Temperature: number;
  Vibration: number;
  SpindleLoad: number;
  CycleTimeSec: number;
  QualityScore: number;
}

/** Envelope pushed over the SSE `/api/factory/events` stream. */
export interface FactoryEventDto {
  Type: 'Telemetry' | 'Command' | 'PartProduced';
  MachineId: string;
  Timestamp: string;
  Telemetry?: TelemetryEventDto;
  Command?: CommandEventDto;
  PartProduced?: PartProducedEventDto;
}

export interface TelemetryEventDto {
  MachineId: string;
  Status: string;
  Timestamp: string;
  PartId: string | null;
  PartStatus: string | null;
  Temperature: number;
  Vibration: number;
  SpindleLoad: number;
  CycleTimeSec: number;
  QualityScore: number;
}

export interface CommandEventDto {
  MachineId: string;
  CommandId: string;
  CommandType: string;
  Parameters: Record<string, string>;
  Timestamp: string;
}

export interface PartProducedEventDto {
  MachineId: string;
  Timestamp: string;
  PartId: string;
  StartedOn: string;
  FinishedOn: string;
}

export interface CreateCommandResponseDto {
  CommandId: string;
  Dispatched: boolean;
}

// ---------------------------------------------------------------------------
// View models
// ---------------------------------------------------------------------------

export type MachineStatus =
  | 'processing'
  | 'idle'
  | 'paused'
  | 'fault'
  | 'stopped'
  | 'unknown';

export interface Telemetry {
  status: MachineStatus;
  timestamp: string;
  partId: string | null;
  partStatus: string | null;
  temperature: number;
  vibration: number;
  spindleLoad: number;
  cycleTimeSec: number;
  qualityScore: number;
}

export interface CommandLogEntry {
  id: string;
  /** Human-friendly label, e.g. "Emergency Stop". */
  typeName: string;
  createdOn: string;
  executedOn: string | null;
  parameters: Record<string, string>;
}

export interface Machine {
  id: string;
  alias: string | null;
  simSpeed: number;
  latest: Telemetry | null;
  /** Most recent first. */
  commands: CommandLogEntry[];
  /** Oldest → newest, capped, used for the sparklines. */
  tempHistory: number[];
  qualityHistory: number[];
}

export interface CommandField {
  label: string;
  key: string;
}

export interface AvailableCommand {
  id: number;
  name: string;
  /** Operator-supplied parameters; empty when the command takes none. */
  fields: CommandField[];
}

/** A part tracked on the production-log side panel. */
export interface Part {
  id: string;
  startedOn: string;
  finishedOn: string | null;
  /** Index of the machine that last reported this part (0-based), if known. */
  lastMachineIndex: number | null;
}

export interface PartLog {
  machineId: string;
  timestamp: string;
  status: MachineStatus;
  partStatus: string | null;
  temperature: number;
  vibration: number;
  spindleLoad: number;
  cycleTimeSec: number;
  qualityScore: number;
}

/**
 * A transient animation of a part travelling along the line. `conveyorIndex`
 * addresses the conveyor segments rendered on the floor: 0 is the raw-stock
 * intake feeding machine 0, 1..N-1 sit between consecutive machines, and N is
 * the finished-goods output after the last machine.
 */
export interface PartFlow {
  id: string;
  conveyorIndex: number;
  partId: string;
  kind: 'intake' | 'transfer' | 'output';
}

export type ConnectionStatus = 'connecting' | 'live' | 'reconnecting' | 'offline';
