import { Component, computed, input } from '@angular/core';
import { GmsIcon, IconName } from '../../icon/icon';

export type StatTone = 'neutral' | 'up' | 'down';

/** Minimal inline sparkline (readable, no chart library). */
@Component({
  selector: 'gms-sparkline',
  standalone: true,
  template: `
    <svg [attr.viewBox]="'0 0 100 ' + h" preserveAspectRatio="none" class="spark" aria-hidden="true">
      <polyline [attr.points]="points()" fill="none" stroke="currentColor" stroke-width="2"
        stroke-linecap="round" stroke-linejoin="round" vector-effect="non-scaling-stroke" />
    </svg>
  `,
  styles: [`:host { display: block; } .spark { width: 100%; height: 100%; display: block; }`]
})
export class GmsSparkline {
  readonly data = input<number[]>([]);
  protected readonly h = 28;

  protected readonly points = computed(() => {
    const d = this.data();
    if (d.length < 2) return '';
    const min = Math.min(...d);
    const max = Math.max(...d);
    const span = max - min || 1;
    const step = 100 / (d.length - 1);
    return d
      .map((v, i) => `${(i * step).toFixed(1)},${(this.h - 2 - ((v - min) / span) * (this.h - 4)).toFixed(1)}`)
      .join(' ');
  });
}

/**
 * Compact stat — label, value, optional delta and sparkline.
 * Minimal and readable; avoids KPI-card overload.
 */
@Component({
  selector: 'gms-stat',
  standalone: true,
  imports: [GmsIcon, GmsSparkline],
  template: `
    <div class="stat">
      <div class="stat__top">
        @if (icon()) { <span class="stat__icon"><gms-icon [name]="icon()!" [size]="16" /></span> }
        <span class="stat__label">{{ label() }}</span>
      </div>
      <div class="stat__row">
        <span class="stat__value t-num">{{ value() }}</span>
        @if (delta()) {
          <span class="stat__delta" [class]="'stat__delta--' + tone()">
            @if (tone() !== 'neutral') { <gms-icon [name]="tone() === 'up' ? 'chevron-down' : 'chevron-down'" [size]="12" /> }
            {{ delta() }}
          </span>
        }
      </div>
      @if (spark().length > 1) {
        <div class="stat__spark" [class]="'stat__spark--' + tone()"><gms-sparkline [data]="spark()" /></div>
      }
    </div>
  `,
  styles: [`
    :host { display: block; }
    .stat {
      background: var(--surface); border: 1px solid var(--border); border-radius: var(--r-lg);
      padding: var(--s-4); display: flex; flex-direction: column; gap: var(--s-2);
    }
    .stat__top { display: flex; align-items: center; gap: var(--s-2); }
    .stat__icon { color: var(--text-subtle); display: flex; }
    .stat__label { font-size: var(--fs-sm); color: var(--text-muted); font-weight: 500; }
    .stat__row { display: flex; align-items: baseline; gap: var(--s-2); }
    .stat__value { font-size: 1.6rem; font-weight: 600; color: var(--text-strong); line-height: 1.1; }
    .stat__delta { font-size: var(--fs-caption); font-weight: 600; display: inline-flex; align-items: center; gap: 2px; }
    .stat__delta--neutral { color: var(--text-muted); }
    .stat__delta--up { color: var(--success); }
    .stat__delta--down { color: var(--danger); }
    .stat__spark { height: 28px; color: var(--brand); opacity: 0.75; }
    .stat__spark--up { color: var(--success); }
    .stat__spark--down { color: var(--danger); }
  `]
})
export class GmsStat {
  readonly label = input('');
  readonly value = input<string | number>('');
  readonly delta = input<string | null>(null);
  readonly tone = input<StatTone>('neutral');
  readonly icon = input<IconName | null>(null);
  readonly spark = input<number[]>([]);
}
