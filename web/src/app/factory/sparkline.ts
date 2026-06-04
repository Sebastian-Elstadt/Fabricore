import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Minimal dependency-free SVG sparkline for a series of recent readings. */
@Component({
  selector: 'app-sparkline',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg
      class="spark"
      [attr.viewBox]="'0 0 ' + width + ' ' + height"
      preserveAspectRatio="none"
      aria-hidden="true"
    >
      @if (area(); as a) {
        <polygon [attr.points]="a" [attr.fill]="'url(#' + gradId() + ')'" />
      }
      @if (line(); as l) {
        <polyline
          [attr.points]="l"
          fill="none"
          [attr.stroke]="color()"
          stroke-width="1.5"
          stroke-linejoin="round"
          stroke-linecap="round"
          vector-effect="non-scaling-stroke"
        />
      }
      <defs>
        <linearGradient [attr.id]="gradId()" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" [attr.stop-color]="color()" stop-opacity="0.35" />
          <stop offset="100%" [attr.stop-color]="color()" stop-opacity="0" />
        </linearGradient>
      </defs>
    </svg>
  `,
  styles: [
    `
      :host {
        display: block;
        width: 100%;
        height: 100%;
      }
      .spark {
        display: block;
        width: 100%;
        height: 100%;
      }
    `,
  ],
})
export class Sparkline {
  readonly data = input<number[]>([]);
  readonly color = input<string>('var(--accent-bright)');
  /** Fixed range; falls back to the data's own min/max when omitted. */
  readonly min = input<number | null>(null);
  readonly max = input<number | null>(null);
  readonly uid = input<string>('spark');

  protected readonly width = 100;
  protected readonly height = 28;

  protected readonly gradId = computed(() => `sg-${this.uid()}`);

  private readonly points = computed(() => {
    const d = this.data();
    if (d.length < 2) return null;

    const lo = this.min() ?? Math.min(...d);
    const hiRaw = this.max() ?? Math.max(...d);
    const hi = hiRaw === lo ? lo + 1 : hiRaw;
    const pad = 3;

    return d.map((v, i) => {
      const x = (i / (d.length - 1)) * this.width;
      const norm = (v - lo) / (hi - lo);
      const y = this.height - pad - norm * (this.height - pad * 2);
      return { x, y };
    });
  });

  protected readonly line = computed(() => {
    const pts = this.points();
    return pts ? pts.map((p) => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ') : null;
  });

  protected readonly area = computed(() => {
    const pts = this.points();
    if (!pts) return null;
    const body = pts.map((p) => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ');
    return `0,${this.height} ${body} ${this.width},${this.height}`;
  });
}
