import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { GmsIcon, IconName } from '../../icon/icon';

export interface LinkItem {
  id: string;
  label: string;
  hint?: string;
  route: string;
  icon: IconName;
  badge?: string;
}

/** Reusable linked-item list — powers Recent Items, Favorites, Pinned, etc. */
@Component({
  selector: 'gms-item-list',
  standalone: true,
  imports: [RouterLink, GmsIcon, TranslocoPipe],
  template: `
    <ul class="il">
      @for (item of items(); track item.id) {
        <li class="il__row">
          <a class="il__link" [routerLink]="item.route">
            <span class="il__icon"><gms-icon [name]="item.icon" [size]="16" /></span>
            <span class="il__main">
              <span class="il__label">{{ item.label }}</span>
              @if (item.hint) { <span class="il__hint">{{ item.hint }}</span> }
            </span>
            @if (item.badge) { <span class="badge badge--neutral">{{ item.badge }}</span> }
          </a>
          @if (removable()) {
            <button type="button" class="il__remove" (click)="remove.emit(item.id)" [attr.aria-label]="'common.remove' | transloco">
              <gms-icon name="close" [size]="15" />
            </button>
          }
        </li>
      }
    </ul>
  `,
  styles: [`
    :host { display: block; }
    .il { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; }
    .il__row { display: flex; align-items: center; }
    .il__link {
      flex: 1; min-width: 0; display: flex; align-items: center; gap: var(--s-3);
      padding: 8px 8px; border-radius: var(--r-sm); text-decoration: none; color: var(--text);
      transition: background var(--motion-fast) var(--ease);
    }
    .il__link:hover { background: var(--surface-hover); }
    .il__icon {
      flex-shrink: 0; width: 30px; height: 30px; border-radius: var(--r-sm);
      background: var(--surface-sunken); color: var(--text-muted);
      display: flex; align-items: center; justify-content: center;
    }
    .il__main { flex: 1; min-width: 0; display: flex; flex-direction: column; }
    .il__label { font-size: var(--fs-body); font-weight: 500; color: var(--text-strong); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .il__hint { font-size: var(--fs-caption); color: var(--text-muted); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .il__remove { width: 28px; height: 28px; border: 0; background: transparent; color: var(--text-subtle); border-radius: var(--r-sm); cursor: pointer; display: flex; align-items: center; justify-content: center; }
    .il__remove:hover { background: var(--danger-bg); color: var(--danger); }
  `]
})
export class GmsItemList {
  readonly items = input<LinkItem[]>([]);
  readonly removable = input(false);
  readonly remove = output<string>();
}
