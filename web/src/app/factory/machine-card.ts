import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
} from '@angular/core';
import { DecimalPipe } from '@angular/common';

import { Machine } from './factory.models';
import { PART_STATUS_LABEL, STATUS_LABEL } from './factory.constants';
import { ClockService, relativeTime } from './clock.service';
import { Sparkline } from './sparkline';

@Component({
  selector: 'app-machine-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, Sparkline],
  templateUrl: './machine-card.html',
  styleUrl: './machine-card.scss',
})
export class MachineCard {
  readonly machine = input.required<Machine>();
  readonly selected = input<boolean>(false);
  readonly select = output<string>();

  private readonly clock = inject(ClockService);

  protected readonly status = computed(() => this.machine().latest?.status ?? 'unknown');
  protected readonly statusLabel = computed(() => STATUS_LABEL[this.status()]);

  protected readonly partStatusLabel = computed(() => {
    const ps = this.machine().latest?.partStatus;
    return ps ? (PART_STATUS_LABEL[ps] ?? ps) : null;
  });

  protected readonly lastSeen = computed(() =>
    relativeTime(this.machine().latest?.timestamp ?? null, this.clock.now()),
  );

  protected readonly qualityClass = computed(() => {
    const q = this.machine().latest?.qualityScore;
    if (q == null) return 'neutral';
    if (q >= 90) return 'good';
    if (q >= 75) return 'warn';
    return 'bad';
  });

  protected readonly tempClass = computed(() => {
    const t = this.machine().latest?.temperature;
    if (t == null) return 'neutral';
    if (t >= 90) return 'bad';
    if (t >= 75) return 'warn';
    return 'neutral';
  });

  protected onActivate(): void {
    this.select.emit(this.machine().id);
  }

  protected onKey(event: KeyboardEvent): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      this.onActivate();
    }
  }
}
