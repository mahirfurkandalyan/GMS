import { Component, input } from '@angular/core';
import { GmsBadge, BadgeTone } from '../badge/badge';

export interface AuditEntry {
  time: string;
  user: string;
  action: string;
  description?: string;
  status?: { label: string; tone: BadgeTone };
}

/**
 * Reusable audit list (audit trail, approval history).
 * `<gms-audit-list [entries]="entries" />`
 */
@Component({
  selector: 'gms-audit-list',
  standalone: true,
  imports: [GmsBadge],
  template: `
    <ul class="al">
      @for (e of entries(); track $index) {
        <li class="al__row">
          <span class="al__avatar">{{ e.user.charAt(0) }}</span>
          <div class="al__main">
            <div class="al__head">
              <span class="al__user">{{ e.user }}</span>
              <span class="al__action">{{ e.action }}</span>
              @if (e.status) { <gms-badge [tone]="e.status.tone">{{ e.status.label }}</gms-badge> }
            </div>
            @if (e.description) { <p class="al__desc">{{ e.description }}</p> }
          </div>
          <span class="al__time">{{ e.time }}</span>
        </li>
      } @empty {
        <li class="al__empty">Kayıt bulunmuyor.</li>
      }
    </ul>
  `,
  styles: [`
    :host { display: block; }
    .al { list-style: none; margin: 0; padding: 0; }
    .al__row { display: flex; align-items: flex-start; gap: var(--s-3); padding: var(--s-3) 0; border-bottom: 1px solid var(--border); }
    .al__row:last-child { border-bottom: 0; }
    .al__avatar {
      flex-shrink: 0; width: 30px; height: 30px; border-radius: 50%;
      background: var(--surface-sunken); color: var(--text-muted);
      display: flex; align-items: center; justify-content: center; font-weight: 600; font-size: 0.78rem;
    }
    .al__main { flex: 1; min-width: 0; }
    .al__head { display: flex; align-items: center; gap: var(--s-2); flex-wrap: wrap; }
    .al__user { font-weight: 600; color: var(--text-strong); font-size: var(--fs-body); }
    .al__action { color: var(--text); font-size: var(--fs-body); }
    .al__desc { margin: 2px 0 0; font-size: var(--fs-sm); color: var(--text-muted); }
    .al__time { font-size: var(--fs-caption); color: var(--text-subtle); white-space: nowrap; font-variant-numeric: tabular-nums; }
    .al__empty { padding: var(--s-5); text-align: center; color: var(--text-muted); font-size: var(--fs-body); }
  `]
})
export class GmsAuditList {
  readonly entries = input<AuditEntry[]>([]);
}
