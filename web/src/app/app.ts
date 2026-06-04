import { Component, OnInit, computed, inject, signal } from '@angular/core';

import { FactoryService } from './factory/factory.service';
import { ConnectionStatus } from './factory/factory.models';
import { STATUS_LABEL } from './factory/factory.constants';
import { MachineCard } from './factory/machine-card';
import { CommandDrawer } from './factory/command-drawer';

const CONNECTION_LABEL: Record<ConnectionStatus, string> = {
  connecting: 'Connecting',
  live: 'Live',
  reconnecting: 'Reconnecting',
  offline: 'Offline',
};

@Component({
  selector: 'app-root',
  imports: [MachineCard, CommandDrawer],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  private readonly factory = inject(FactoryService);

  protected readonly machines = this.factory.machines;
  protected readonly availableCommands = this.factory.availableCommands;
  protected readonly connection = this.factory.connection;

  protected readonly connectionLabel = computed(() => CONNECTION_LABEL[this.connection()]);

  private readonly selectedId = signal<string | null>(null);
  protected readonly selectedMachine = computed(
    () => this.machines().find((m) => m.id === this.selectedId()) ?? null,
  );

  /** Live tally for the header: how many machines sit in each status. */
  protected readonly tally = computed(() => {
    const counts = { processing: 0, idle: 0, paused: 0, fault: 0, stopped: 0, unknown: 0 };
    for (const m of this.machines()) {
      counts[m.latest?.status ?? 'unknown']++;
    }
    return counts;
  });

  protected readonly statusLabel = STATUS_LABEL;

  ngOnInit(): void {
    void this.factory.connect();
  }

  protected onSelect(id: string): void {
    this.selectedId.set(id);
  }

  protected onCloseDrawer(): void {
    this.selectedId.set(null);
  }
}
