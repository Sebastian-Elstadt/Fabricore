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

interface LogState {
  status: 'loading' | 'ready' | 'error';
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
  /** Per-part log fetch state, keyed by part id. */
  private readonly logCache = signal<Record<string, LogState>>({});

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
    return this.logCache()[id] ?? null;
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
    const cached = this.logCache()[partId];
    if (cached && cached.status !== 'error') return;

    this.patchLog(partId, { status: 'loading', logs: [] });
    try {
      const logs = await this.factory.getPartLogs(partId);
      const ordered = logs
        .slice()
        .sort((a, b) => Date.parse(a.timestamp) - Date.parse(b.timestamp));
      this.patchLog(partId, { status: 'ready', logs: ordered });
    } catch {
      this.patchLog(partId, { status: 'error', logs: [] });
    }
  }

  protected retry(partId: string): void {
    this.logCache.update((c) => {
      const next = { ...c };
      delete next[partId];
      return next;
    });
    void this.loadLogs(partId);
  }

  private patchLog(partId: string, state: LogState): void {
    this.logCache.update((c) => ({ ...c, [partId]: state }));
  }

  protected shortPart(partId: string): string {
    return partId.replace(/^PART-/i, '');
  }
}
