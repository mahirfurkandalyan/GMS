import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { GmsIcon, IconName } from '../../icon/icon';
import { SearchService } from '../../../core/search.service';
import { CommandService, CommandEntry } from './command.service';
import { LanguageService } from '../../../core/language.service';

interface DisplayCommand extends CommandEntry {
  displayLabel: string;
  displayHint: string;
}

/** Ctrl/⌘+K command palette — reads from the CommandService registry + global search. */
@Component({
  selector: 'gms-command-palette',
  standalone: true,
  imports: [GmsIcon, TranslocoPipe],
  host: { '(document:keydown)': 'onKey($event)' },
  template: `
    @if (open()) {
      <div class="cp-ovl" (click)="close()">
        <div class="cp" role="dialog" aria-modal="true" [attr.aria-label]="'commandPalette.title' | transloco" (click)="$event.stopPropagation()">
          <div class="cp__search">
            <gms-icon name="search" [size]="18" />
            <input
              type="text"
              class="cp__input"
              [placeholder]="'commandPalette.placeholder' | transloco"
              [value]="query()"
              (input)="onInput($event)"
              autocomplete="off" />
            <span class="cp__esc">ESC</span>
          </div>
          <div class="cp__list">
            @if (results().length === 0) {
              <div class="cp__empty">{{ 'commandPalette.noResults' | transloco }}</div>
            } @else {
              @for (item of results(); track item.id; let i = $index) {
                @if (showGroupHeader(i)) { <div class="cp__group">{{ ('commandPalette.group.' + item.group) | transloco }}</div> }
                <button
                  type="button"
                  class="cp__item"
                  [class.cp__item--active]="i === active()"
                  (mouseenter)="active.set(i)"
                  (click)="run(item)">
                  <gms-icon [name]="item.icon" [size]="17" />
                  <span class="cp__label">{{ item.displayLabel }}</span>
                  <span class="cp__hint">{{ item.displayHint }}</span>
                </button>
              }
            }
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .cp-ovl {
      position: fixed; inset: 0; z-index: var(--z-overlay); background: var(--overlay);
      display: flex; align-items: flex-start; justify-content: center; padding-top: 12vh;
      animation: ovl-in var(--motion-fast) var(--ease);
    }
    .cp {
      width: 100%; max-width: 580px; background: var(--surface);
      border: 1px solid var(--border); border-radius: var(--r-lg); box-shadow: var(--shadow-lg);
      overflow: hidden; animation: cp-in var(--motion) var(--ease-out);
    }
    .cp__search { display: flex; align-items: center; gap: var(--s-3); padding: var(--s-4); border-bottom: 1px solid var(--border); color: var(--text-subtle); }
    .cp__input { flex: 1; border: 0; background: transparent; font-family: inherit; font-size: 1rem; color: var(--text); }
    .cp__input:focus { outline: none; }
    .cp__esc { font-size: 0.68rem; color: var(--text-subtle); border: 1px solid var(--border-strong); border-radius: 5px; padding: 2px 6px; }
    .cp__list { max-height: 360px; overflow-y: auto; padding: var(--s-2); }
    .cp__empty { padding: var(--s-5); text-align: center; color: var(--text-muted); font-size: var(--fs-body); }
    .cp__group { padding: 8px 12px 4px; font-size: var(--fs-label); font-weight: 600; letter-spacing: 0.06em; text-transform: uppercase; color: var(--text-subtle); }
    .cp__item {
      display: flex; align-items: center; gap: var(--s-3); width: 100%;
      padding: 10px 12px; border: 0; background: transparent; border-radius: var(--r-sm);
      cursor: pointer; text-align: left; font: inherit; color: var(--text);
    }
    .cp__item--active { background: var(--brand-subtle); color: var(--brand-text); }
    .cp__item--active gms-icon { color: var(--brand); }
    .cp__item gms-icon { color: var(--text-subtle); }
    .cp__label { font-size: var(--fs-body); font-weight: 500; }
    .cp__hint { margin-left: auto; font-size: var(--fs-caption); color: var(--text-subtle); }
    @keyframes ovl-in { from { opacity: 0; } to { opacity: 1; } }
    @keyframes cp-in { from { opacity: 0; transform: translateY(-8px); } to { opacity: 1; transform: none; } }
  `]
})
export class GmsCommandPalette {
  private readonly router = inject(Router);
  private readonly searchSvc = inject(SearchService);
  private readonly commandSvc = inject(CommandService);
  private readonly transloco = inject(TranslocoService);
  private readonly language = inject(LanguageService);

  protected readonly open = signal(false);
  protected readonly query = signal('');
  protected readonly active = signal(0);

  protected readonly results = computed<DisplayCommand[]>(() => {
    this.language.current();
    const q = this.query().trim().toLocaleLowerCase('tr');

    const base: DisplayCommand[] = this.commandSvc.commands().map((c) => ({
      ...c,
      displayLabel: c.labelKey ? this.transloco.translate(c.labelKey) : (c.label ?? ''),
      displayHint: c.hintKey ? this.transloco.translate(c.hintKey) : (c.hint ?? '')
    }));

    const commands = q
      ? base.filter(
          (c) =>
            c.displayLabel.toLocaleLowerCase('tr').includes(q) ||
            c.displayHint.toLocaleLowerCase('tr').includes(q)
        )
      : base;

    const found: DisplayCommand[] = q
      ? this.searchSvc
          .search(q)
          .filter((r) => r.route)
          .map((r) => ({
            id: 'search-' + r.label,
            label: r.label,
            hint: r.category,
            displayLabel: r.label,
            displayHint: r.category,
            icon: 'search' as IconName,
            route: r.route as string,
            group: 'results' as const
          }))
      : [];

    return [...commands, ...found];
  });

  showGroupHeader(index: number): boolean {
    const items = this.results();
    return index === 0 || items[index].group !== items[index - 1].group;
  }

  onKey(event: KeyboardEvent): void {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      this.toggle();
      return;
    }
    if (!this.open()) return;

    if (event.key === 'Escape') {
      event.preventDefault();
      this.close();
    } else if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.active.update((i) => Math.min(i + 1, this.results().length - 1));
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.active.update((i) => Math.max(i - 1, 0));
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const item = this.results()[this.active()];
      if (item) this.run(item);
    }
  }

  onInput(event: Event): void {
    this.query.set((event.target as HTMLInputElement).value);
    this.active.set(0);
  }

  toggle(): void {
    this.open.update((v) => !v);
    if (this.open()) {
      this.query.set('');
      this.active.set(0);
    }
  }

  close(): void {
    this.open.set(false);
  }

  run(item: CommandEntry): void {
    this.close();
    if (item.run) {
      item.run();
    } else if (item.route) {
      this.router.navigateByUrl(item.route);
    }
  }
}
