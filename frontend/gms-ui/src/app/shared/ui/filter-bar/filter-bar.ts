import { Component, computed, inject, input, model, output, signal } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { GmsIcon } from '../../icon/icon';
import { GmsMenu, MenuItem } from '../menu/menu';
import { LanguageService } from '../../../core/language.service';

export interface QuickFilter {
  id: string;
  label: string;
}

/**
 * Enterprise filter toolbar: search, quick filters, advanced panel, reset, saved views.
 * `<gms-filter-bar [(search)]="q" [quickFilters]="qf" [(active)]="active" (reset)="onReset()">
 *    <div advanced>…selects…</div>
 *    <button actions gmsButton …>…</button>
 *  </gms-filter-bar>`
 */
@Component({
  selector: 'gms-filter-bar',
  standalone: true,
  imports: [GmsIcon, GmsMenu, TranslocoPipe],
  template: `
    <div class="fb">
      <div class="fb__row">
        <div class="fb__search">
          <gms-icon name="search" [size]="17" />
          <input
            type="text"
            [placeholder]="placeholder()"
            [value]="search()"
            (input)="search.set($any($event.target).value)" />
        </div>

        @if (quickFilters().length) {
          <div class="fb__quick">
            @for (qf of quickFilters(); track qf.id) {
              <button
                type="button"
                class="fb__chip"
                [class.fb__chip--on]="active().includes(qf.id)"
                (click)="toggle(qf.id)">
                {{ qf.label }}
              </button>
            }
          </div>
        }

        <span class="fb__spacer"></span>

        <ng-content select="[actions]"></ng-content>

        @if (hasAdvanced()) {
          <button type="button" class="gms-btn gms-btn--ghost gms-btn--sm" (click)="advancedOpen.set(!advancedOpen())">
            <gms-icon name="filter" [size]="15" /> {{ 'filterBar.advanced' | transloco }}
            <gms-icon [name]="advancedOpen() ? 'chevron-down' : 'chevron-right'" [size]="14" />
          </button>
        }

        <gms-menu [items]="savedViewItems()" (select)="viewSelected.emit($event)">
          <button trigger type="button" class="gms-btn gms-btn--ghost gms-btn--sm">
            <gms-icon name="folder" [size]="15" /> {{ 'filterBar.views' | transloco }}
          </button>
        </gms-menu>

        @if (canReset()) {
          <button type="button" class="gms-btn gms-btn--ghost gms-btn--sm" (click)="onReset()">{{ 'filterBar.reset' | transloco }}</button>
        }
      </div>

      @if (advancedOpen() && hasAdvanced()) {
        <div class="fb__advanced">
          <ng-content select="[advanced]"></ng-content>
        </div>
      }
    </div>
  `,
  styles: [`
    :host { display: block; }
    .fb { display: flex; flex-direction: column; gap: var(--s-3); }
    .fb__row { display: flex; align-items: center; gap: var(--s-2); flex-wrap: wrap; }
    .fb__search {
      display: flex; align-items: center; gap: var(--s-2); flex: 1; min-width: 220px; max-width: 380px;
      height: 38px; padding: 0 10px; color: var(--text-subtle);
      border: 1px solid var(--border-strong); border-radius: var(--r-sm); background: var(--surface);
      transition: border-color var(--motion-fast) var(--ease), box-shadow var(--motion-fast) var(--ease);
    }
    .fb__search:focus-within { border-color: var(--brand); box-shadow: var(--focus-ring); }
    .fb__search input { flex: 1; border: 0; background: transparent; font: inherit; font-size: var(--fs-body); color: var(--text); }
    .fb__search input:focus { outline: none; }
    .fb__search input::placeholder { color: var(--text-subtle); }
    .fb__quick { display: flex; gap: 6px; flex-wrap: wrap; }
    .fb__chip {
      height: 30px; padding: 0 12px; border: 1px solid var(--border-strong); border-radius: var(--r-pill);
      background: var(--surface); font: inherit; font-size: var(--fs-sm); font-weight: 500; color: var(--text-muted);
      cursor: pointer; transition: all var(--motion-fast) var(--ease);
    }
    .fb__chip:hover { border-color: var(--text-subtle); color: var(--text); }
    .fb__chip--on { background: var(--brand-subtle); border-color: transparent; color: var(--brand-text); }
    .fb__spacer { flex: 1; }
    .fb__advanced {
      display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: var(--s-3);
      padding: var(--s-4); background: var(--surface-sunken); border: 1px solid var(--border); border-radius: var(--r-md);
      animation: fb-in var(--motion-fast) var(--ease-out);
    }
    @keyframes fb-in { from { opacity: 0; transform: translateY(-4px); } to { opacity: 1; transform: none; } }
  `]
})
export class GmsFilterBar {
  private readonly transloco = inject(TranslocoService);
  private readonly language = inject(LanguageService);

  readonly search = model<string>('');
  readonly active = model<string[]>([]);
  readonly placeholder = input(this.transloco.translate('filterBar.searchPlaceholder'));
  readonly quickFilters = input<QuickFilter[]>([]);
  readonly hasAdvanced = input(false);
  readonly savedViews = input<string[] | null>(null);

  readonly reset = output<void>();
  readonly viewSelected = output<string>();

  protected readonly advancedOpen = signal(false);

  protected readonly savedViewItems = computed<MenuItem[]>(() => {
    this.language.current();
    const views = this.savedViews() ?? [
      this.transloco.translate('filterBar.allRecords'),
      this.transloco.translate('filterBar.myView')
    ];
    return [
      ...views.map((v) => ({ label: v, value: v })),
      { label: this.transloco.translate('filterBar.saveViewSoon'), value: '__save', disabled: true }
    ];
  });

  protected readonly canReset = computed(
    () => !!this.search().trim() || this.active().length > 0
  );

  toggle(id: string): void {
    this.active.update((list) =>
      list.includes(id) ? list.filter((x) => x !== id) : [...list, id]
    );
  }

  onReset(): void {
    this.search.set('');
    this.active.set([]);
    this.reset.emit();
  }
}
