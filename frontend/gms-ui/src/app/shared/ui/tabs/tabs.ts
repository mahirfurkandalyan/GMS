import { Component, input, model, output } from '@angular/core';
import { GmsIcon, IconName } from '../../icon/icon';

export interface TabItem {
  id: string;
  label: string;
  icon?: IconName;
  badge?: string | number;
  disabled?: boolean;
}

/**
 * Enterprise tab bar (Release/Change/Execution/Documents/Audit detail pages).
 * `<gms-tabs [tabs]="tabs" [(active)]="active" />` then switch content on `active()`.
 */
@Component({
  selector: 'gms-tabs',
  standalone: true,
  imports: [GmsIcon],
  template: `
    <div class="tabs" role="tablist">
      @for (tab of tabs(); track tab.id) {
        <button
          type="button"
          role="tab"
          class="tabs__tab"
          [class.tabs__tab--active]="active() === tab.id"
          [attr.aria-selected]="active() === tab.id"
          [disabled]="tab.disabled"
          (click)="select(tab)">
          @if (tab.icon) { <gms-icon [name]="tab.icon" [size]="16" /> }
          <span>{{ tab.label }}</span>
          @if (tab.badge != null) { <span class="tabs__badge">{{ tab.badge }}</span> }
        </button>
      }
    </div>
  `,
  styles: [`
    :host { display: block; }
    .tabs {
      display: flex; gap: 2px; border-bottom: 1px solid var(--border);
      overflow-x: auto; scrollbar-width: none;
    }
    .tabs::-webkit-scrollbar { display: none; }
    .tabs__tab {
      display: inline-flex; align-items: center; gap: var(--s-2);
      padding: 10px 14px; border: 0; background: transparent;
      font: inherit; font-size: var(--fs-body); font-weight: 500; color: var(--text-muted);
      cursor: pointer; position: relative; white-space: nowrap;
      border-bottom: 2px solid transparent; margin-bottom: -1px;
      transition: color var(--motion-fast) var(--ease);
    }
    .tabs__tab gms-icon { color: var(--text-subtle); }
    .tabs__tab:hover:not(:disabled) { color: var(--text-strong); }
    .tabs__tab:hover:not(:disabled) gms-icon { color: var(--text-muted); }
    .tabs__tab:disabled { opacity: var(--disabled-opacity); cursor: not-allowed; }
    .tabs__tab--active { color: var(--brand-text); border-bottom-color: var(--brand); }
    .tabs__tab--active gms-icon { color: var(--brand); }
    .tabs__badge {
      font-size: var(--fs-label); font-weight: 600; min-width: 18px; height: 18px; padding: 0 5px;
      display: inline-flex; align-items: center; justify-content: center;
      border-radius: var(--r-pill); background: var(--neutral-bg); color: var(--neutral);
    }
    .tabs__tab--active .tabs__badge { background: var(--brand-subtle); color: var(--brand-text); }
  `]
})
export class GmsTabs {
  readonly tabs = input<TabItem[]>([]);
  readonly active = model<string>('');
  readonly tabChange = output<string>();

  select(tab: TabItem): void {
    if (tab.disabled) return;
    this.active.set(tab.id);
    this.tabChange.emit(tab.id);
  }
}
