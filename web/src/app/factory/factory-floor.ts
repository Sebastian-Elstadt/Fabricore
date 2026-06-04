import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
} from '@angular/core';
import { DecimalPipe } from '@angular/common';

import { Machine, MachineStatus, PartFlow } from './factory.models';
import { STATUS_LABEL } from './factory.constants';
import { ClockService, relativeTime } from './clock.service';
import { Sparkline } from './sparkline';

/** Ordered nodes laid out left→right across the production line. */
type FloorNode =
  | { kind: 'intake' }
  | { kind: 'output' }
  | { kind: 'conveyor'; index: number }
  | { kind: 'station'; machine: Machine; index: number };

@Component({
  selector: 'app-factory-floor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, Sparkline],
  templateUrl: './factory-floor.html',
  styleUrl: './factory-floor.scss',
})
export class FactoryFloor {
  readonly machines = input.required<Machine[]>();
  readonly flows = input<PartFlow[]>([]);
  readonly selectedId = input<string | null>(null);
  readonly select = output<string>();

  private readonly clock = inject(ClockService);

  /** Builds the interleaved intake → conveyor/station… → output sequence. */
  protected readonly nodes = computed<FloorNode[]>(() => {
    const machines = this.machines();
    const out: FloorNode[] = [{ kind: 'intake' }];
    machines.forEach((machine, index) => {
      out.push({ kind: 'conveyor', index });
      out.push({ kind: 'station', machine, index });
    });
    out.push({ kind: 'conveyor', index: machines.length });
    out.push({ kind: 'output' });
    return out;
  });

  protected flowsFor(index: number): PartFlow[] {
    return this.flows().filter((f) => f.conveyorIndex === index);
  }

  protected status(m: Machine): MachineStatus {
    return m.latest?.status ?? 'unknown';
  }

  protected statusLabel(m: Machine): string {
    return STATUS_LABEL[this.status(m)];
  }

  protected isActive(m: Machine): boolean {
    return this.status(m) === 'processing';
  }

  protected tempTone(m: Machine): string {
    const t = m.latest?.temperature;
    if (t == null) return 'neutral';
    if (t >= 90) return 'bad';
    if (t >= 75) return 'warn';
    return 'neutral';
  }

  protected qualityTone(m: Machine): string {
    const q = m.latest?.qualityScore;
    if (q == null) return 'neutral';
    if (q >= 90) return 'good';
    if (q >= 75) return 'warn';
    return 'bad';
  }

  protected lastSeen(m: Machine): string {
    return relativeTime(m.latest?.timestamp ?? null, this.clock.now());
  }

  /** Strip the verbose "PART-" prefix for compact chips. */
  protected shortPart(partId: string): string {
    return partId.replace(/^PART-/i, '');
  }

  protected onSelect(id: string): void {
    this.select.emit(id);
  }

  protected onKey(event: KeyboardEvent, id: string): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.onSelect(id);
    }
  }
}
