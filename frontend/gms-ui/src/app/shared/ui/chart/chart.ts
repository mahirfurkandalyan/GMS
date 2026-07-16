import { Component, computed, input } from '@angular/core';
import { BadgeTone } from '../badge/badge';

export type ChartType = 'bar' | 'line' | 'donut';

export interface ChartDatum {
  label: string;
  value: number;
  tone?: BadgeTone;
}

const TONE_COLOR: Record<BadgeTone, string> = {
  neutral: 'var(--text-subtle)',
  info: 'var(--info, #1f6fd6)',
  success: 'var(--success, #1a7f4b)',
  warning: 'var(--warning, #b7791f)',
  danger: 'var(--danger, #c0392b)'
};

const DEFAULT_CYCLE: BadgeTone[] = ['info', 'success', 'warning', 'danger', 'neutral'];

interface Bar { x: number; y: number; w: number; h: number; color: string; label: string; value: number; }
interface Slice { d: string; color: string; label: string; value: number; pct: number; }

/**
 * Dependency-free, responsive SVG chart primitive for the analytics layer.
 * Supports bar / line / donut. Colors resolve from the shared badge tone palette
 * so charts stay consistent with the rest of the design system. Built to accept
 * real data later without any template change.
 *
 * `<gms-chart type="donut" [data]="riskData()" />`
 */
@Component({
  selector: 'gms-chart',
  standalone: true,
  template: `
    @switch (type()) {
      @case ('donut') {
        <div class="chart chart--donut">
          <svg viewBox="0 0 120 120" class="chart__svg" role="img" [attr.aria-label]="ariaLabel()">
            @if (total() === 0) {
              <circle cx="60" cy="60" r="46" fill="none" stroke="var(--surface-sunken)" stroke-width="16" />
            }
            @for (s of slices(); track s.label) {
              <path [attr.d]="s.d" fill="none" [attr.stroke]="s.color" stroke-width="16" stroke-linecap="butt" />
            }
            <text x="60" y="56" text-anchor="middle" class="chart__donut-value">{{ total() }}</text>
            <text x="60" y="72" text-anchor="middle" class="chart__donut-label">{{ centerLabel() }}</text>
          </svg>
          <ul class="chart__legend">
            @for (s of slices(); track s.label) {
              <li>
                <span class="chart__dot" [style.background]="s.color"></span>
                <span class="chart__legend-label">{{ s.label }}</span>
                <span class="chart__legend-value">{{ s.value }} · %{{ s.pct }}</span>
              </li>
            }
          </ul>
        </div>
      }
      @case ('line') {
        <div class="chart" [style.--chart-h.px]="height()">
          <svg [attr.viewBox]="'0 0 ' + vbW + ' ' + vbH()" preserveAspectRatio="none" class="chart__svg chart__svg--plot" role="img" [attr.aria-label]="ariaLabel()">
            @for (g of gridLines(); track g) { <line [attr.x1]="pad" [attr.y1]="g" [attr.x2]="vbW - pad" [attr.y2]="g" class="chart__grid" vector-effect="non-scaling-stroke" /> }
            <polygon [attr.points]="areaPoints()" [attr.fill]="lineFill()" opacity="0.12" />
            <polyline [attr.points]="linePoints()" fill="none" [attr.stroke]="lineStroke()" stroke-width="2"
              stroke-linecap="round" stroke-linejoin="round" vector-effect="non-scaling-stroke" />
            @for (p of linePointList(); track p.label) {
              <circle [attr.cx]="p.x" [attr.cy]="p.y" r="2.5" [attr.fill]="lineStroke()" vector-effect="non-scaling-stroke" />
            }
          </svg>
          <div class="chart__xaxis">
            @for (p of linePointList(); track p.label) { <span>{{ p.label }}</span> }
          </div>
        </div>
      }
      @default {
        <div class="chart" [style.--chart-h.px]="height()">
          <svg [attr.viewBox]="'0 0 ' + vbW + ' ' + vbH()" preserveAspectRatio="none" class="chart__svg chart__svg--plot" role="img" [attr.aria-label]="ariaLabel()">
            @for (g of gridLines(); track g) { <line [attr.x1]="pad" [attr.y1]="g" [attr.x2]="vbW - pad" [attr.y2]="g" class="chart__grid" vector-effect="non-scaling-stroke" /> }
            @for (b of bars(); track b.label) {
              <rect [attr.x]="b.x" [attr.y]="b.y" [attr.width]="b.w" [attr.height]="b.h" [attr.fill]="b.color" rx="1.5" />
            }
          </svg>
          <div class="chart__xaxis">
            @for (b of bars(); track b.label) { <span>{{ b.label }}</span> }
          </div>
        </div>
      }
    }
  `,
  styles: [`
    :host { display: block; }
    .chart { display: flex; flex-direction: column; gap: var(--s-2); width: 100%; }
    .chart__svg { width: 100%; display: block; }
    .chart__svg--plot { height: var(--chart-h, 160px); }
    .chart__grid { stroke: var(--border); stroke-dasharray: 2 3; }
    .chart__xaxis { display: flex; justify-content: space-between; padding: 0 2px; }
    .chart__xaxis span { font-size: 0.66rem; color: var(--text-subtle); flex: 1; text-align: center; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

    .chart--donut { flex-direction: row; align-items: center; gap: var(--s-5); flex-wrap: wrap; }
    .chart--donut .chart__svg { width: 130px; height: 130px; flex-shrink: 0; }
    .chart__donut-value { font-size: 20px; font-weight: 700; fill: var(--text-strong); }
    .chart__donut-label { font-size: 8px; fill: var(--text-subtle); text-transform: uppercase; letter-spacing: .04em; }
    .chart__legend { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 8px; flex: 1; min-width: 150px; }
    .chart__legend li { display: flex; align-items: center; gap: var(--s-2); font-size: var(--fs-sm); }
    .chart__dot { width: 9px; height: 9px; border-radius: 2px; flex-shrink: 0; }
    .chart__legend-label { color: var(--text); flex: 1; }
    .chart__legend-value { color: var(--text-subtle); font-size: var(--fs-caption); font-variant-numeric: tabular-nums; }
  `]
})
export class GmsChart {
  readonly type = input<ChartType>('bar');
  readonly data = input<ChartDatum[]>([]);
  readonly height = input(160);
  readonly centerLabel = input('Toplam');
  readonly ariaLabel = input('Grafik');

  protected readonly vbW = 300;
  protected readonly pad = 8;
  protected readonly vbH = computed(() => this.height());

  private readonly color = (d: ChartDatum, i: number): string =>
    TONE_COLOR[d.tone ?? DEFAULT_CYCLE[i % DEFAULT_CYCLE.length]];

  protected readonly total = computed(() => this.data().reduce((s, d) => s + d.value, 0));

  private readonly maxVal = computed(() => Math.max(1, ...this.data().map((d) => d.value)));

  protected readonly gridLines = computed(() => {
    const h = this.vbH();
    const top = this.pad;
    const bottom = h - this.pad;
    const n = 3;
    return Array.from({ length: n + 1 }, (_, i) => +(top + ((bottom - top) / n) * i).toFixed(1));
  });

  // ── Bar ──
  protected readonly bars = computed<Bar[]>(() => {
    const data = this.data();
    if (!data.length) return [];
    const h = this.vbH();
    const top = this.pad;
    const bottom = h - this.pad;
    const plotW = this.vbW - this.pad * 2;
    const slot = plotW / data.length;
    const bw = Math.min(34, slot * 0.6);
    const max = this.maxVal();
    return data.map((d, i) => {
      const barH = ((bottom - top) * d.value) / max;
      return {
        x: +(this.pad + slot * i + (slot - bw) / 2).toFixed(1),
        y: +(bottom - barH).toFixed(1),
        w: +bw.toFixed(1),
        h: +Math.max(1, barH).toFixed(1),
        color: this.color(d, i),
        label: d.label,
        value: d.value
      };
    });
  });

  // ── Line ──
  protected readonly linePointList = computed(() => {
    const data = this.data();
    if (!data.length) return [] as { x: number; y: number; label: string }[];
    const h = this.vbH();
    const top = this.pad;
    const bottom = h - this.pad;
    const plotW = this.vbW - this.pad * 2;
    const max = this.maxVal();
    const step = data.length > 1 ? plotW / (data.length - 1) : 0;
    return data.map((d, i) => ({
      x: +(this.pad + step * i).toFixed(1),
      y: +(bottom - ((bottom - top) * d.value) / max).toFixed(1),
      label: d.label
    }));
  });
  protected readonly linePoints = computed(() => this.linePointList().map((p) => `${p.x},${p.y}`).join(' '));
  protected readonly areaPoints = computed(() => {
    const pts = this.linePointList();
    if (!pts.length) return '';
    const bottom = this.vbH() - this.pad;
    return `${pts[0].x},${bottom} ${pts.map((p) => `${p.x},${p.y}`).join(' ')} ${pts[pts.length - 1].x},${bottom}`;
  });
  protected readonly lineStroke = computed(() => this.color(this.data()[0] ?? { label: '', value: 0 }, 0));
  protected readonly lineFill = computed(() => this.lineStroke());

  // ── Donut ──
  protected readonly slices = computed<Slice[]>(() => {
    const data = this.data();
    const total = this.total();
    if (!total) return [];
    const cx = 60, cy = 60, r = 46;
    let angle = -Math.PI / 2;
    return data.map((d, i) => {
      const frac = d.value / total;
      const start = angle;
      const end = angle + frac * Math.PI * 2;
      angle = end;
      const large = end - start > Math.PI ? 1 : 0;
      const x1 = +(cx + r * Math.cos(start)).toFixed(2);
      const y1 = +(cy + r * Math.sin(start)).toFixed(2);
      const x2 = +(cx + r * Math.cos(end)).toFixed(2);
      const y2 = +(cy + r * Math.sin(end)).toFixed(2);
      // Nearly-full circle: split so the arc renders.
      const d1 = frac > 0.999
        ? `M ${cx - r} ${cy} A ${r} ${r} 0 1 1 ${cx + r} ${cy} A ${r} ${r} 0 1 1 ${cx - r} ${cy}`
        : `M ${x1} ${y1} A ${r} ${r} 0 ${large} 1 ${x2} ${y2}`;
      return { d: d1, color: this.color(d, i), label: d.label, value: d.value, pct: Math.round(frac * 100) };
    });
  });
}
