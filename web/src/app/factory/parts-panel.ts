import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { DecimalPipe } from '@angular/common';

import { Part, PartLog } from './factory.models';
import { PART_STATUS_LABEL, STATUS_LABEL } from './factory.constants';
import { FactoryService } from './factory.service';
import { ClockService, relativeTime } from './clock.service';

type LogFetchStatus = 'loading' | 'ready' | 'error';

interface LogState {
  status: LogFetchStatus;
  logs: PartLog[];
}

@Component({
  selector: 'app-parts-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe],
  templateUrl: './parts-panel.html',
  styleUrl: './parts-panel.scss',
})
export class PartsPanel {
  readonly parts = input.required<Part[]>();
  /** Total number of machines in the line, used to render stage progress. */
  readonly stageCount = input<number>(4);

  private readonly factory = inject(FactoryService);
  private readonly clock = inject(ClockService);

  protected readonly expandedId = signal<string | null>(null);
  /** Per-part fetch status, keyed by part id. The log entries themselves live
   * in the {@link FactoryService} so SSE packets can extend them live. */
  private readonly logStatus = signal<Record<string, LogFetchStatus>>({});

  protected readonly stages = computed(() =>
    Array.from({ length: this.stageCount() }, (_, i) => i),
  );

  protected readonly producedCount = computed(
    () => this.parts().filter((p) => p.finishedOn).length,
  );

  protected isExpanded(id: string): boolean {
    return this.expandedId() === id;
  }

  protected logStateFor(id: string): LogState | null {
    const status = this.logStatus()[id];
    if (!status) return null;
    return { status, logs: this.factory.partLogs()[id] ?? [] };
  }

  protected partStatusLabel(part: Part): string {
    return part.finishedOn ? 'Completed' : 'In Progress';
  }

  protected logPartStatus(ps: string | null): string {
    return ps ? (PART_STATUS_LABEL[ps] ?? ps) : '—';
  }

  protected statusLabel(status: PartLog['status']): string {
    return STATUS_LABEL[status];
  }

  protected relStarted(part: Part): string {
    return relativeTime(part.startedOn, this.clock.now());
  }

  protected relLog(iso: string): string {
    return relativeTime(iso, this.clock.now());
  }

  /** Highlight stage dots up to the last machine the part reached. */
  protected reached(part: Part, stage: number): boolean {
    return part.lastMachineIndex != null && stage <= part.lastMachineIndex;
  }

  protected toggle(part: Part): void {
    if (this.expandedId() === part.id) {
      this.expandedId.set(null);
      return;
    }
    this.expandedId.set(part.id);
    void this.loadLogs(part.id);
  }

  private async loadLogs(partId: string): Promise<void> {
    const status = this.logStatus()[partId];
    if (status === 'loading' || status === 'ready') return;

    this.setStatus(partId, 'loading');
    try {
      await this.factory.loadPartLogs(partId);
      this.setStatus(partId, 'ready');
    } catch {
      this.setStatus(partId, 'error');
    }
  }

  protected retry(partId: string): void {
    this.factory.clearPartLogs(partId);
    this.logStatus.update((c) => {
      const next = { ...c };
      delete next[partId];
      return next;
    });
    void this.loadLogs(partId);
  }

  private setStatus(partId: string, status: LogFetchStatus): void {
    this.logStatus.update((c) => ({ ...c, [partId]: status }));
  }

  protected shortPart(partId: string): string {
    return partId.replace(/^PART-/i, '');
  }
}
