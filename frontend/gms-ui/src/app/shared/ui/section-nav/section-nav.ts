import { Component, inject, input, model } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { GmsIcon, IconName } from '../../icon/icon';

export interface SectionNavItem {
  key: string;
  label: string;
  icon: IconName;
  /** Optional trailing pill, e.g. "Yakında" or a count. */
  badge?: string;
}

export interface SectionNavGroup {
  title: string | null;
  items: SectionNavItem[];
}

/**
 * Reusable in-page secondary navigation (grouped, sticky). Shared by the
 * Administration and Reports centers — a single source for the left rail.
 *
 * `<gms-section-nav [groups]="groups" [(active)]="active" />`
 */
@Component({
  selector: 'gms-section-nav',
  standalone: true,
  imports: [GmsIcon],
  template: `
    <nav class="snav" [attr.aria-label]="ariaLabel()">
      @for (group of groups(); track group.title) {
        @if (group.title) { <span class="snav__group">{{ group.title }}</span> }
        @for (item of group.items; track item.key) {
          <button type="button" class="snav__item" [class.snav__item--active]="active() === item.key" (click)="active.set(item.key)">
            <gms-icon [name]="item.icon" [size]="16" />
            <span class="snav__label">{{ item.label }}</span>
            @if (item.badge) { <span class="snav__badge">{{ item.badge }}</span> }
          </button>
        }
      }
    </nav>
  `,
  styles: [`
    :host { display: block; }
    .snav {
      position: sticky; top: var(--s-5);
      display: flex; flex-direction: column; gap: 2px;
      padding: var(--s-3);
      background: var(--surface); border: 1px solid var(--border); border-radius: var(--r-lg);
      box-shadow: var(--shadow-xs);
    }
    .snav__group {
      font-size: 0.62rem; text-transform: uppercase; letter-spacing: 0.06em; font-weight: 700;
      color: var(--text-subtle); padding: var(--s-3) var(--s-3) var(--s-2);
    }
    .snav__item {
      display: flex; align-items: center; gap: var(--s-2);
      padding: 8px 10px; border: 0; border-radius: var(--r-sm);
      background: transparent; color: var(--text-muted); font: inherit; font-size: var(--fs-sm);
      text-align: left; cursor: pointer; width: 100%;
      transition: background var(--motion-fast) var(--ease), color var(--motion-fast) var(--ease);
    }
    .snav__item gms-icon { color: var(--text-subtle); flex-shrink: 0; }
    .snav__item:hover { background: var(--surface-hover); color: var(--text); }
    .snav__item--active { background: var(--brand-subtle); color: var(--brand-text); }
    .snav__item--active gms-icon { color: var(--brand); }
    .snav__label { flex: 1; font-weight: 500; }
    .snav__badge {
      font-size: 0.6rem; font-weight: 700; text-transform: uppercase; letter-spacing: .04em;
      color: var(--text-subtle); background: var(--surface-sunken); border: 1px solid var(--border);
      padding: 1px 5px; border-radius: var(--r-pill);
    }
    @media (max-width: 620px) {
      .snav { position: static; }
    }
  `]
})
export class GmsSectionNav {
  private readonly transloco = inject(TranslocoService);

  readonly groups = input<SectionNavGroup[]>([]);
  readonly active = model<string>('');
  readonly ariaLabel = input(this.transloco.translate('common.sections'));
}
