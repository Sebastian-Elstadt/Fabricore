/**
 * Wire models mirror the JSON shapes returned by the API exactly (PascalCase,
 * `PropertyNamingPolicy = null` on the server). They are mapped into the
 * camelCase view-models below before reaching the UI.
 */

export interface FactoryStateSnapshotDto {
  Machines: FactoryMachineDto[];
  AvailableCommands: AvailableCommandDto[];
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
}

/** Envelope pushed over the SSE `/api/factory/events` stream. */
export interface FactoryEventDto {
  Type: 'Telemetry' | 'Command';
  MachineId: string;
  Timestamp: string;
  Telemetry?: TelemetryEventDto;
  Command?: CommandEventDto;
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

export interface AvailableCommand {
  id: number;
  name: string;
}

export type ConnectionStatus = 'connecting' | 'live' | 'reconnecting' | 'offline';
