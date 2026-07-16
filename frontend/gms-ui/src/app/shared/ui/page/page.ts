import { Component, inject, input, model } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { GmsIcon } from '../../icon/icon';

/**
 * Universal page scaffold — every screen shares the exact same structure:
 * header · toolbar · content · (optional) right context panel · (optional) footer.
 *
 * `<gms-page [contextPanel]="true" panelTitle="Bağlam">
 *    <gms-page-header header … />
 *    <div toolbar>…</div>
 *    …content…
 *    <div panel>…context sections…</div>
 *    <div footer>…</div>
 *  </gms-page>`
 */
@Component({
  selector: 'gms-page',
  standalone: true,
  imports: [GmsIcon, TranslocoPipe],
  template: `
    <div class="page" [class.page--paneled]="contextPanel() && panelOpen()">
      <div class="page__header"><ng-content select="[header]"></ng-content></div>

      <div class="page__toolbar"><ng-content select="[toolbar]"></ng-content></div>

      <div class="page__body">
        <div class="page__content"><ng-content></ng-content></div>

        @if (contextPanel() && panelOpen()) {
          <aside class="page__panel" [attr.aria-label]="'common.contextPanel' | transloco">
            <div class="page__panel-head">
              <span class="page__panel-title">{{ panelTitle() }}</span>
              <button type="button" class="page__panel-btn" (click)="panelOpen.set(false)" [attr.aria-label]="'common.closePanel' | transloco">
                <gms-icon name="chevron-right" [size]="16" />
              </button>
            </div>
            <div class="page__panel-body"><ng-content select="[panel]"></ng-content></div>
          </aside>
        }
      </div>

      <div class="page__footer"><ng-content select="[footer]"></ng-content></div>
    </div>

    @if (contextPanel() && !panelOpen()) {
      <button type="button" class="page__panel-reopen" (click)="panelOpen.set(true)" [title]="'common.openContextPanel' | transloco">
        <gms-icon name="sidebar" [size]="16" />
      </button>
    }
  `,
  styles: [`
    :host { display: block; }
    .page { display: flex; flex-direction: column; gap: var(--s-5); max-width: 1440px; }
    .page__header:empty, .page__toolbar:empty, .page__footer:empty { display: none; }
    .page__body { display: grid; grid-template-columns: 1fr; gap: var(--s-5); align-items: start; }
    .page--paneled .page__body { grid-template-columns: minmax(0, 1fr) 320px; }
    .page__content { min-width: 0; display: flex; flex-direction: column; gap: var(--s-5); }
    .page__panel {
      position: sticky; top: var(--s-5); align-self: start;
      background: var(--surface); border: 1px solid var(--border); border-radius: var(--r-lg);
      box-shadow: var(--shadow-xs); overflow: hidden;
      animation: panel-in var(--motion) var(--ease-out);
    }
    .page__panel-head { display: flex; align-items: center; justify-content: space-between; padding: var(--s-3) var(--s-4); border-bottom: 1px solid var(--border); }
    .page__panel-title { font-size: var(--fs-sm); font-weight: 600; color: var(--text-strong); text-transform: uppercase; letter-spacing: 0.04em; }
    .page__panel-btn { width: 28px; height: 28px; border: 0; background: transparent; color: var(--text-subtle); border-radius: var(--r-sm); cursor: pointer; display: flex; align-items: center; justify-content: center; }
    .page__panel-btn:hover { background: var(--surface-hover); color: var(--text); }
    .page__panel-body { display: flex; flex-direction: column; }
    .page__panel-reopen {
      position: fixed; right: 16px; bottom: 16px; z-index: var(--z-nav);
      width: 42px; height: 42px; border-radius: 50%; border: 1px solid var(--border);
      background: var(--surface); color: var(--brand); box-shadow: var(--shadow-md); cursor: pointer;
      display: flex; align-items: center; justify-content: center;
    }
    @keyframes panel-in { from { opacity: 0; transform: translateX(8px); } to { opacity: 1; transform: none; } }
    @media (max-width: 1100px) {
      .page--paneled .page__body { grid-template-columns: 1fr; }
      .page__panel { position: static; }
    }
  `]
})
export class GmsPage {
  private readonly transloco = inject(TranslocoService);

  readonly contextPanel = input(false);
  readonly panelTitle = input(this.transloco.translate('common.context'));
  readonly panelOpen = model(true);
}
