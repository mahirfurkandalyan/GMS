import { Component, input, model, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { GmsIcon, IconName } from '../../icon/icon';

/**
 * Reusable workspace widget shell.
 * Supports collapse/expand, refresh, loading skeleton, empty slot, actions slot,
 * and a drag-handle placeholder (drag-and-drop architecture, not yet wired).
 *
 * `<gms-widget title="Görevlerim" icon="check" [loading]="loading()" [empty]="!items().length"
 *      [refreshable]="true" (refresh)="reload()">
 *    …content…
 *    <div empty>…</div>
 *  </gms-widget>`
 */
@Component({
  selector: 'gms-widget',
  standalone: true,
  imports: [GmsIcon, TranslocoPipe],
  template: `
    <section class="widget" [class.widget--collapsed]="collapsed()">
      <header class="widget__head">
        @if (draggable()) {
          <span class="widget__handle" aria-hidden="true" [title]="'common.moveSoon' | transloco">
            <gms-icon name="sort" [size]="14" />
          </span>
        }
        @if (icon()) { <span class="widget__icon"><gms-icon [name]="icon()!" [size]="16" /></span> }
        <h3 class="widget__title">{{ title() }}</h3>
        @if (count() != null) { <span class="widget__count">{{ count() }}</span> }
        <span class="widget__spacer"></span>

        <ng-content select="[actions]"></ng-content>

        @if (refreshable()) {
          <button type="button" class="widget__btn" [class.is-spinning]="loading()" (click)="refresh.emit()" [attr.aria-label]="'common.refresh' | transloco">
            <gms-icon name="clock" [size]="15" />
          </button>
        }
        @if (collapsible()) {
          <button type="button" class="widget__btn" (click)="collapsed.set(!collapsed())"
            [attr.aria-label]="(collapsed() ? 'common.expand' : 'common.collapse') | transloco">
            <gms-icon [name]="collapsed() ? 'chevron-right' : 'chevron-down'" [size]="16" />
          </button>
        }
      </header>

      @if (!collapsed()) {
        <div class="widget__body">
          @if (loading()) {
            <div class="widget__skel">
              <span class="skeleton skeleton--line" style="width:70%"></span>
              <span class="skeleton skeleton--line" style="width:90%"></span>
              <span class="skeleton skeleton--line" style="width:55%"></span>
            </div>
          } @else if (empty()) {
            <ng-content select="[empty]"></ng-content>
          } @else {
            <ng-content></ng-content>
          }
        </div>
      }
    </section>
  `,
  styles: [`
    :host { display: block; }
    .widget {
      background: var(--surface); border: 1px solid var(--border);
      border-radius: var(--r-lg); box-shadow: var(--shadow-xs);
      display: flex; flex-direction: column; overflow: hidden;
      transition: box-shadow var(--motion) var(--ease);
    }
    .widget:hover { box-shadow: var(--shadow-sm); }
    .widget__head {
      display: flex; align-items: center; gap: var(--s-2);
      padding: var(--s-3) var(--s-4); min-height: 46px;
      border-bottom: 1px solid var(--border);
    }
    .widget--collapsed .widget__head { border-bottom: 0; }
    .widget__handle { color: var(--text-subtle); cursor: grab; display: flex; opacity: 0.6; }
    .widget__handle:active { cursor: grabbing; }
    .widget__icon { color: var(--text-muted); display: flex; }
    .widget__title { font-size: var(--fs-body); font-weight: 600; color: var(--text-strong); }
    .widget__count {
      font-size: var(--fs-label); font-weight: 600; min-width: 18px; height: 18px; padding: 0 5px;
      display: inline-flex; align-items: center; justify-content: center;
      border-radius: var(--r-pill); background: var(--neutral-bg); color: var(--neutral);
    }
    .widget__spacer { flex: 1; }
    .widget__btn {
      width: 28px; height: 28px; border: 0; background: transparent; color: var(--text-subtle);
      border-radius: var(--r-sm); display: flex; align-items: center; justify-content: center; cursor: pointer;
      transition: background var(--motion-fast) var(--ease), color var(--motion-fast) var(--ease);
    }
    .widget__btn:hover { background: var(--surface-hover); color: var(--text); }
    .widget__btn.is-spinning gms-icon { animation: gms-spin 0.8s linear infinite; }
    .widget__body { padding: var(--s-4); }
    .widget__skel { display: flex; flex-direction: column; gap: var(--s-3); }
    @keyframes gms-spin { to { transform: rotate(360deg); } }
  `]
})
export class GmsWidget {
  readonly title = input('');
  readonly icon = input<IconName | null>(null);
  readonly count = input<number | string | null>(null);
  readonly loading = input(false);
  readonly empty = input(false);
  readonly collapsible = input(true);
  readonly refreshable = input(false);
  readonly draggable = input(false);
  readonly collapsed = model(false);
  readonly refresh = output<void>();
}
