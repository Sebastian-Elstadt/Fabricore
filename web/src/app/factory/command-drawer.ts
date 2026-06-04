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

import { AvailableCommand, CommandField, Machine } from './factory.models';
import { COMMAND_META, PART_STATUS_LABEL, STATUS_LABEL } from './factory.constants';
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

  /** Which parameterised command currently has its field form expanded. */
  protected readonly openCmdId = signal<number | null>(null);
  /** Live values entered into the expanded form, keyed by field key. */
  protected readonly fieldValues = signal<Record<string, string>>({});

  private feedbackTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    // Reset transient panel state whenever a different machine is opened.
    let lastId: string | null = null;
    effect(() => {
      const m = this.machine();
      if (m.id === lastId) return;
      lastId = m.id;
      this.openCmdId.set(null);
      this.fieldValues.set({});
      this.feedback.set(null);
      this.busyId.set(null);
    });
  }

  protected readonly status = computed(() => this.machine().latest?.status ?? 'unknown');
  protected readonly statusLabel = computed(() => STATUS_LABEL[this.status()]);

  protected readonly partStatusLabel = computed(() => {
    const ps = this.machine().latest?.partStatus;
    return ps ? (PART_STATUS_LABEL[ps] ?? ps) : null;
  });

  /** The parameterised command whose field form is currently expanded. */
  protected readonly openCommand = computed(
    () => this.availableCommands().find((c) => c.id === this.openCmdId()) ?? null,
  );

  protected severity(id: number): string {
    return COMMAND_META[id]?.severity ?? 'info';
  }

  protected hasFields(cmd: AvailableCommand): boolean {
    return cmd.fields.length > 0;
  }

  protected commandCode(id: number): string {
    return COMMAND_META[id]?.code ?? '';
  }

  protected relTime(iso: string | null): string {
    return relativeTime(iso, this.clock.now());
  }

  protected onCommandClick(cmd: AvailableCommand): void {
    if (this.hasFields(cmd)) {
      this.toggleForm(cmd);
      return;
    }
    void this.dispatch(cmd.id, cmd.name);
  }

  private toggleForm(cmd: AvailableCommand): void {
    if (this.openCmdId() === cmd.id) {
      this.openCmdId.set(null);
      return;
    }
    // Seed the form with sensible defaults drawn from live machine state.
    const seed: Record<string, string> = {};
    for (const f of cmd.fields) seed[f.key] = this.defaultFor(f.key);
    this.fieldValues.set(seed);
    this.openCmdId.set(cmd.id);
  }

  /** Best-effort prefill for known field keys; blank for anything unfamiliar. */
  private defaultFor(key: string): string {
    const m = this.machine();
    switch (key) {
      case 'sim_speed':
        return round(m.simSpeed, 1).toString();
      case 'spindle_load':
        return Math.round(m.latest?.spindleLoad ?? 55).toString();
      default:
        return '';
    }
  }

  protected fieldValue(key: string): string {
    return this.fieldValues()[key] ?? '';
  }

  protected setField(key: string, value: string): void {
    this.fieldValues.update((v) => ({ ...v, [key]: value }));
  }

  protected canSubmit(cmd: AvailableCommand): boolean {
    return cmd.fields.every((f) => this.fieldValue(f.key).trim() !== '');
  }

  protected submitFields(cmd: AvailableCommand): void {
    const params: Record<string, string> = {};
    for (const f of cmd.fields) params[f.key] = this.fieldValue(f.key).trim();
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
      if (params) this.openCmdId.set(null);
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

  protected trackField(_: number, field: CommandField): string {
    return field.key;
  }
}

function round(n: number, digits: number): number {
  const f = 10 ** digits;
  return Math.round(n * f) / f;
}
