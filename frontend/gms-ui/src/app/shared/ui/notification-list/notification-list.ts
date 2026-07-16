import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { GmsIcon } from '../../icon/icon';

export interface NotificationRow {
  id: string;
  categoryLabel: string;
  badgeClass: string;
  priority: 'high' | 'normal' | 'low';
  title: string;
  detail: string;
  time: string;
  read: boolean;
  actionLabel?: string;
  actionRoute?: string;
}

/** Enterprise notification list — unread, priority, category, timestamp, action. */
@Component({
  selector: 'gms-notification-list',
  standalone: true,
  imports: [RouterLink, GmsIcon, TranslocoPipe],
  template: `
    <ul class="nl" [class.nl--compact]="compact()">
      @for (n of items(); track n.id) {
        <li class="nl__row" [class.nl__row--unread]="!n.read" (click)="markRead.emit(n.id)">
          @if (!n.read) { <span class="nl__unread" aria-hidden="true"></span> }
          @if (n.priority === 'high') { <span class="nl__prio" [title]="'common.highPriority' | transloco"></span> }
          <div class="nl__main">
            <div class="nl__head">
              <span class="badge" [class]="n.badgeClass">{{ n.categoryLabel }}</span>
              <span class="nl__title">{{ n.title }}</span>
              <span class="nl__time">{{ n.time }}</span>
            </div>
            @if (!compact()) { <p class="nl__detail">{{ n.detail }}</p> }
            @if (n.actionLabel && n.actionRoute) {
              <a class="nl__action" [routerLink]="n.actionRoute" (click)="$event.stopPropagation()">
                {{ n.actionLabel }} <gms-icon name="chevron-right" [size]="13" />
              </a>
            }
          </div>
        </li>
      } @empty {
        <li class="nl__empty">{{ 'notificationList.empty' | transloco }}</li>
      }
    </ul>
  `,
  styles: [`
    :host { display: block; }
    .nl { list-style: none; margin: 0; padding: 0; }
    .nl__row {
      position: relative; display: flex; align-items: flex-start; gap: var(--s-3);
      padding: var(--s-3) var(--s-3) var(--s-3) var(--s-4); border-bottom: 1px solid var(--border);
      cursor: pointer; transition: background var(--motion-fast) var(--ease);
    }
    .nl__row:last-child { border-bottom: 0; }
    .nl__row:hover { background: var(--surface-hover); }
    .nl__row--unread { background: color-mix(in srgb, var(--brand-subtle) 60%, transparent); }
    .nl__row--unread:hover { background: var(--brand-subtle); }
    .nl__unread { position: absolute; left: 6px; top: 50%; width: 7px; height: 7px; border-radius: 50%; background: var(--brand); transform: translateY(-50%); }
    .nl__prio { width: 3px; align-self: stretch; border-radius: 3px; background: var(--danger); flex-shrink: 0; }
    .nl__main { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 3px; }
    .nl__head { display: flex; align-items: center; gap: var(--s-2); flex-wrap: wrap; }
    .nl__title { font-size: var(--fs-body); font-weight: 600; color: var(--text-strong); }
    .nl__time { margin-left: auto; font-size: var(--fs-caption); color: var(--text-subtle); white-space: nowrap; }
    .nl__detail { margin: 0; font-size: var(--fs-sm); color: var(--text-muted); }
    .nl__action { display: inline-flex; align-items: center; gap: 3px; font-size: var(--fs-sm); font-weight: 600; color: var(--brand-text); text-decoration: none; width: fit-content; }
    .nl__action:hover { text-decoration: underline; }
    .nl--compact .nl__row { padding-block: var(--s-2); }
    .nl__empty { padding: var(--s-5); text-align: center; color: var(--text-muted); font-size: var(--fs-body); }
  `]
})
export class GmsNotificationList {
  readonly items = input<NotificationRow[]>([]);
  readonly compact = input(false);
  readonly markRead = output<string>();
}
