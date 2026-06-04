import { Injectable, signal } from '@angular/core';

/** A single shared 1s ticker so relative timestamps across the UI stay live
 *  without every component spinning up its own interval. */
@Injectable({ providedIn: 'root' })
export class ClockService {
  private readonly _now = signal(Date.now());
  readonly now = this._now.asReadonly();

  constructor() {
    setInterval(() => this._now.set(Date.now()), 1000);
  }
}

/** Compact "moments ago" / "12s ago" / "4m ago" relative formatter. */
export function relativeTime(iso: string | null, now: number): string {
  if (!iso) return '—';
  const then = Date.parse(iso);
  if (Number.isNaN(then)) return '—';
  const secs = Math.max(0, Math.round((now - then) / 1000));
  if (secs < 2) return 'just now';
  if (secs < 60) return `${secs}s ago`;
  const mins = Math.floor(secs / 60);
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  return `${Math.floor(hrs / 24)}d ago`;
}
