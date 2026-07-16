import { Component, computed, input, output, signal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { GmsIcon, IconName } from '../../icon/icon';

export interface MenuItem {
  label: string;
  value: string;
  icon?: IconName;
  tone?: 'default' | 'danger';
  disabled?: boolean;
}

/**
 * Dropdown / action / context menu.
 * `<gms-menu [items]="actions" (select)="onAction($event)" />`
 * Optional custom trigger: project an element with the `trigger` attribute.
 */
@Component({
  selector: 'gms-menu',
  standalone: true,
  imports: [GmsIcon, TranslocoPipe],
  template: `
    <span class="menu">
      <span class="menu__trigger" (click)="toggle($event)">
        <ng-content select="[trigger]"></ng-content>
        @if (defaultTrigger()) {
          <button type="button" class="gms-btn gms-btn--ghost gms-btn--icon gms-btn--sm" [attr.aria-label]="'common.actions' | transloco">
            <gms-icon name="chevron-down" [size]="16" />
          </button>
        }
      </span>

      @if (open()) {
        <span class="menu__backdrop" (click)="open.set(false)"></span>
        <div class="menu__panel" [class]="'menu__panel--' + align()" role="menu">
          @for (item of items(); track item.value) {
            <button
              type="button"
              class="menu__item"
              [class.menu__item--danger]="item.tone === 'danger'"
              [disabled]="item.disabled"
              role="menuitem"
              (click)="choose(item)">
              @if (item.icon) { <gms-icon [name]="item.icon" [size]="16" /> }
              <span>{{ item.label }}</span>
            </button>
          }
        </div>
      }
    </span>
  `,
  styles: [`
    .menu { position: relative; display: inline-flex; }
    .menu__trigger { display: inline-flex; }
    .menu__backdrop { position: fixed; inset: 0; z-index: var(--z-dropdown); }
    .menu__panel {
      position: absolute; top: calc(100% + 6px); min-width: 190px;
      background: var(--surface); border: 1px solid var(--border);
      border-radius: var(--r-md); box-shadow: var(--shadow-lg);
      padding: 6px; z-index: calc(var(--z-dropdown) + 1);
      animation: menu-in var(--motion-fast) var(--ease-out);
    }
    .menu__panel--end { right: 0; }
    .menu__panel--start { left: 0; }
    .menu__item {
      display: flex; align-items: center; gap: var(--s-2); width: 100%;
      padding: 8px 10px; border: 0; background: transparent; border-radius: var(--r-sm);
      font: inherit; font-size: var(--fs-sm); font-weight: 500; color: var(--text);
      cursor: pointer; text-align: left;
    }
    .menu__item:hover:not(:disabled) { background: var(--surface-hover); color: var(--text-strong); }
    .menu__item:disabled { opacity: var(--disabled-opacity); cursor: not-allowed; }
    .menu__item--danger { color: var(--danger); }
    .menu__item--danger:hover { background: var(--danger-bg); }
    .menu__item gms-icon { color: var(--text-subtle); }
    @keyframes menu-in { from { opacity: 0; transform: translateY(-4px); } to { opacity: 1; transform: none; } }
  `]
})
export class GmsMenu {
  readonly items = input<MenuItem[]>([]);
  readonly align = input<'start' | 'end'>('end');
  /** Set false when projecting a custom `[trigger]` element. */
  readonly defaultTrigger = input(true);
  readonly select = output<string>();

  protected readonly open = signal(false);

  toggle(event: Event): void {
    event.stopPropagation();
    this.open.update((v) => !v);
  }

  choose(item: MenuItem): void {
    if (item.disabled) return;
    this.open.set(false);
    this.select.emit(item.value);
  }
}
