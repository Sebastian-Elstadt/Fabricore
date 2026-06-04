import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { DecimalPipe } from '@angular/common';

import { AvailableCommand, Machine } from './factory.models';
import {
  COMMAND_META,
  PART_STATUS_LABEL,
  STATUS_LABEL,
} from './factory.constants';
import { FactoryService } from './factory.service';
import { ClockService, relativeTime } from './clock.service';

interface Feedback {
  text: string;
  tone: 'ok' | 'queued' | 'error';
}

@Component({
  selector: 'app-command-drawer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe],
  templateUrl: './command-drawer.html',
  styleUrl: './command-drawer.scss',
})
export class CommandDrawer {
  readonly machine = input.required<Machine>();
  readonly availableCommands = input<AvailableCommand[]>([]);
  readonly close = output<void>();

  private readonly factory = inject(FactoryService);
  private readonly clock = inject(ClockService);

  protected readonly busyId = signal<number | null>(null);
  protected readonly feedback = signal<Feedback | null>(null);
  protected readonly showAdjust = signal(false);

  protected readonly simSpeedInput = signal(1);
  protected readonly spindleInput = signal(55);

  private feedbackTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    // Reset transient panel state whenever a different machine is opened.
    let lastId: string | null = null;
    effect(() => {
      const m = this.machine();
      if (m.id === lastId) return;
      lastId = m.id;
      this.showAdjust.set(false);
      this.feedback.set(null);
      this.busyId.set(null);
      this.simSpeedInput.set(round(m.simSpeed, 1));
      this.spindleInput.set(Math.round(m.latest?.spindleLoad ?? 55));
    });
  }

  protected readonly status = computed(() => this.machine().latest?.status ?? 'unknown');
  protected readonly statusLabel = computed(() => STATUS_LABEL[this.status()]);

  protected readonly partStatusLabel = computed(() => {
    const ps = this.machine().latest?.partStatus;
    return ps ? (PART_STATUS_LABEL[ps] ?? ps) : null;
  });

  protected readonly adjustCommand = computed(
    () => this.availableCommands().find((c) => this.hasParams(c.id)) ?? null,
  );

  protected severity(id: number): string {
    return COMMAND_META[id]?.severity ?? 'info';
  }

  protected hasParams(id: number): boolean {
    return COMMAND_META[id]?.hasParams ?? false;
  }

  protected commandCode(id: number): string {
    return COMMAND_META[id]?.code ?? '';
  }

  protected relTime(iso: string | null): string {
    return relativeTime(iso, this.clock.now());
  }

  protected onCommandClick(cmd: AvailableCommand): void {
    if (this.hasParams(cmd.id)) {
      this.showAdjust.update((v) => !v);
      return;
    }
    void this.dispatch(cmd.id, cmd.name);
  }

  protected submitAdjust(cmd: AvailableCommand): void {
    const params = {
      sim_speed: clamp(this.simSpeedInput(), 0.1, 20).toString(),
      spindle_load: clamp(this.spindleInput(), 0, 100).toString(),
    };
    void this.dispatch(cmd.id, cmd.name, params);
  }

  private async dispatch(
    id: number,
    label: string,
    params?: Record<string, string>,
  ): Promise<void> {
    if (this.busyId() !== null) return;
    this.busyId.set(id);
    try {
      const res = await this.factory.sendCommand(this.machine().id, id, params);
      this.setFeedback(
        res.Dispatched
          ? { text: `${label} dispatched to ${this.machine().id}.`, tone: 'ok' }
          : { text: `${label} queued — ${this.machine().id} is offline.`, tone: 'queued' },
      );
    } catch {
      this.setFeedback({ text: `Failed to send ${label}.`, tone: 'error' });
    } finally {
      this.busyId.set(null);
    }
  }

  private setFeedback(fb: Feedback): void {
    this.feedback.set(fb);
    if (this.feedbackTimer) clearTimeout(this.feedbackTimer);
    this.feedbackTimer = setTimeout(() => this.feedback.set(null), 5000);
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.close.emit();
  }

  protected onClose(): void {
    this.close.emit();
  }
}

function clamp(n: number, lo: number, hi: number): number {
  if (!Number.isFinite(n)) return lo;
  return Math.min(hi, Math.max(lo, n));
}

function round(n: number, digits: number): number {
  const f = 10 ** digits;
  return Math.round(n * f) / f;
}
