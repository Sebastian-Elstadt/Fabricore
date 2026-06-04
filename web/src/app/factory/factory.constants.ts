import { MachineStatus } from './factory.models';

/** Dev runs the Angular dev-server separately, so it must target the API host
 *  directly. In prod the SPA is served from inside the API, so calls are
 *  relative to the same origin. */
import { isDevMode } from '@angular/core';
export const API_BASE = isDevMode() ? 'http://localhost:8000' : '';

/** Maps the server's `MachineCommandType` enum (the numeric id used both as the
 *  `AvailableCommand.Id` and the value POSTed back as `Type`) to presentation
 *  metadata and the wire code emitted on the command event stream. */
export interface CommandMeta {
  /** Wire code, e.g. "EMERGENCY_STOP" — used to label live command events. */
  code: string;
  severity: 'go' | 'info' | 'warn' | 'danger';
  /** Whether the command carries adjustable parameters. */
  hasParams: boolean;
}

export const COMMAND_META: Record<number, CommandMeta> = {
  0: { code: 'PAUSE', severity: 'warn', hasParams: false },
  1: { code: 'RESUME', severity: 'go', hasParams: false },
  2: { code: 'EMERGENCY_STOP', severity: 'danger', hasParams: false },
  3: { code: 'COOL_DOWN', severity: 'info', hasParams: false },
  4: { code: 'ADJUST_SPEED', severity: 'info', hasParams: true },
  5: { code: 'INJECT_DEFECT', severity: 'danger', hasParams: false },
  6: { code: 'ASSIGN_PART', severity: 'go', hasParams: false },
};

/** Reverse lookup: wire code → human label, for command events that only carry
 *  the code string. */
export const CODE_TO_LABEL: Record<string, string> = {
  PAUSE: 'Pause',
  RESUME: 'Resume',
  EMERGENCY_STOP: 'Emergency Stop',
  COOL_DOWN: 'Cool Down',
  ADJUST_SPEED: 'Adjust Sim Speed',
  INJECT_DEFECT: 'Inject Sim Defect',
  ASSIGN_PART: 'Assign Part',
};

export const KNOWN_STATUSES: readonly MachineStatus[] = [
  'processing',
  'idle',
  'paused',
  'fault',
  'stopped',
];

export function normalizeStatus(raw: string | null | undefined): MachineStatus {
  const s = (raw ?? '').toLowerCase();
  return (KNOWN_STATUSES as readonly string[]).includes(s)
    ? (s as MachineStatus)
    : 'unknown';
}

export const STATUS_LABEL: Record<MachineStatus, string> = {
  processing: 'Processing',
  idle: 'Idle',
  paused: 'Paused',
  fault: 'Fault',
  stopped: 'Stopped',
  unknown: 'No Signal',
};

export const PART_STATUS_LABEL: Record<string, string> = {
  in_progress: 'In Progress',
  completed: 'Completed',
  quarantined: 'Quarantined',
};
