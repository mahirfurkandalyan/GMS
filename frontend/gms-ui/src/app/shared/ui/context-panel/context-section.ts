import { Component, input, model } from '@angular/core';
import { GmsIcon, IconName } from '../../icon/icon';

/**
 * A collapsible section inside a page context panel.
 * Reusable for Recent Activity / Related Objects / Attachments / Notes / Approvals / History.
 * `<gms-context-section title="İlgili Nesneler" icon="folder" [count]="3">…</gms-context-section>`
 */
@Component({
  selector: 'gms-context-section',
  standalone: true,
  imports: [GmsIcon],
  template: `
    <section class="cs">
      <button type="button" class="cs__head" (click)="collapsed.set(!collapsed())" [attr.aria-expanded]="!collapsed()">
        @if (icon()) { <gms-icon [name]="icon()!" [size]="15" /> }
        <span class="cs__title">{{ title() }}</span>
        @if (count() != null) { <span class="cs__count">{{ count() }}</span> }
        <span class="cs__spacer"></span>
        <gms-icon [name]="collapsed() ? 'chevron-right' : 'chevron-down'" [size]="15" />
      </button>
      @if (!collapsed()) {
        <div class="cs__body"><ng-content></ng-content></div>
      }
    </section>
  `,
  styles: [`
    :host { display: block; border-bottom: 1px solid var(--border); }
    :host:last-child { border-bottom: 0; }
    .cs__head {
      display: flex; align-items: center; gap: var(--s-2); width: 100%;
      padding: var(--s-3) var(--s-4); border: 0; background: transparent; cursor: pointer;
      font: inherit; color: var(--text); text-align: left;
    }
    .cs__head:hover { background: var(--surface-hover); }
    .cs__head gms-icon { color: var(--text-subtle); }
    .cs__title { font-size: var(--fs-sm); font-weight: 600; color: var(--text-strong); }
    .cs__count { font-size: var(--fs-label); font-weight: 600; min-width: 18px; height: 18px; padding: 0 5px; display: inline-flex; align-items: center; justify-content: center; border-radius: var(--r-pill); background: var(--neutral-bg); color: var(--neutral); }
    .cs__spacer { flex: 1; }
    .cs__body { padding: 0 var(--s-4) var(--s-4); }
  `]
})
export class GmsContextSection {
  readonly title = input('');
  readonly icon = input<IconName | null>(null);
  readonly count = input<number | string | null>(null);
  readonly collapsed = model(false);
}
