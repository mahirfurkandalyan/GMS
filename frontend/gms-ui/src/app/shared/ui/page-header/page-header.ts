import { Component, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { GmsBreadcrumbs, Crumb } from '../breadcrumbs/breadcrumbs';
import { GmsBadge, BadgeTone } from '../badge/badge';
import { GmsIcon } from '../../icon/icon';

export interface PageStatus {
  label: string;
  tone: BadgeTone;
}

export interface HeaderContext {
  key: string;
  label: string;
}

/**
 * Smart, universal page header — breadcrumb, title, description, status,
 * current-context chips, last-updated, primary + secondary actions.
 * `<gms-page-header title="…" subtitle="…" [breadcrumbs]="c" [status]="…"
 *      [context]="[{key:'Proje',label:'EBR Migration'}]" lastUpdated="2 saat önce">
 *    <button secondary …>…</button><button actions gmsButton variant="primary">…</button>
 *  </gms-page-header>`
 */
@Component({
  selector: 'gms-page-header',
  standalone: true,
  imports: [GmsBreadcrumbs, GmsBadge, GmsIcon, TranslocoPipe],
  template: `
    <header class="ph">
      @if (breadcrumbs().length) {
        <gms-breadcrumbs [items]="breadcrumbs()" />
      }
      <div class="ph__row">
        <div class="ph__titles">
          <div class="ph__title-line">
            <h1 class="ph__title">{{ title() }}</h1>
            @if (status(); as s) { <gms-badge [tone]="s.tone" [dot]="true">{{ s.label }}</gms-badge> }
          </div>
          @if (subtitle()) { <p class="ph__sub">{{ subtitle() }}</p> }

          @if (context().length || lastUpdated()) {
            <div class="ph__context">
              @for (c of context(); track c.key) {
                <span class="ph__chip"><span class="ph__chip-key">{{ c.key }}</span> {{ c.label }}</span>
              }
              @if (lastUpdated()) {
                <span class="ph__updated"><gms-icon name="clock" [size]="13" /> {{ 'common.lastUpdated' | transloco }}: {{ lastUpdated() }}</span>
              }
            </div>
          }
        </div>
        <div class="ph__actions">
          <ng-content select="[secondary]"></ng-content>
          <ng-content select="[actions]"></ng-content>
        </div>
      </div>
    </header>
  `,
  styles: [`
    :host { display: block; }
    .ph { display: flex; flex-direction: column; gap: var(--s-3); }
    .ph__row { display: flex; align-items: flex-start; justify-content: space-between; gap: var(--s-4); flex-wrap: wrap; }
    .ph__title-line { display: flex; align-items: center; gap: var(--s-3); }
    .ph__title { font-size: var(--fs-h1); font-weight: 600; letter-spacing: -0.014em; color: var(--text-strong); margin: 0 0 4px; }
    .ph__sub { margin: 0; color: var(--text-muted); font-size: var(--fs-body); max-width: 640px; }
    .ph__actions { display: flex; align-items: center; gap: var(--s-2); }
    .ph__context { display: flex; flex-wrap: wrap; align-items: center; gap: var(--s-2); margin-top: var(--s-2); }
    .ph__chip { display: inline-flex; align-items: center; gap: 5px; height: 24px; padding: 0 9px; font-size: var(--fs-caption); font-weight: 500; color: var(--text); background: var(--surface-sunken); border: 1px solid var(--border); border-radius: var(--r-pill); }
    .ph__chip-key { color: var(--text-subtle); }
    .ph__updated { display: inline-flex; align-items: center; gap: 5px; font-size: var(--fs-caption); color: var(--text-subtle); }
  `]
})
export class GmsPageHeader {
  readonly title = input<string>('');
  readonly subtitle = input<string | null>(null);
  readonly breadcrumbs = input<Crumb[]>([]);
  readonly status = input<PageStatus | null>(null);
  readonly context = input<HeaderContext[]>([]);
  readonly lastUpdated = input<string | null>(null);
}
