import { Component, input } from '@angular/core';
import { GmsIcon, IconName } from '../../icon/icon';

export interface ActivityItem {
  actor: string;
  action: string;
  target?: string;
  time: string;
  icon?: IconName;
  /** Optional group label (e.g. "Bugün", "Dün") for grouped rendering. */
  group?: string;
}

/** Reusable activity feed with optional day-grouping and a connected timeline rail. */
@Component({
  selector: 'gms-activity-feed',
  standalone: true,
  imports: [GmsIcon],
  template: `
    <div class="af">
      @for (item of items(); track $index; let last = $last) {
        @if (item.group && showHeader($index)) {
          <div class="af__group">{{ item.group }}</div>
        }
        <div class="af__item" [class.af__item--last]="last">
          <span class="af__rail">
            <span class="af__icon">
              @if (item.icon) { <gms-icon [name]="item.icon" [size]="14" /> }
              @else { <span class="af__initial">{{ item.actor.charAt(0) }}</span> }
            </span>
          </span>
          <div class="af__body">
            <span class="af__text"><strong>{{ item.actor }}</strong> {{ item.action }}
              @if (item.target) { <span class="af__target">{{ item.target }}</span> }
            </span>
            <span class="af__time">{{ item.time }}</span>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    :host { display: block; }
    .af__group {
      font-size: var(--fs-label); font-weight: 600; letter-spacing: 0.06em; text-transform: uppercase;
      color: var(--text-subtle); padding: var(--s-3) 0 var(--s-2);
    }
    .af__item { display: flex; gap: var(--s-3); position: relative; }
    .af__rail { position: relative; display: flex; flex-direction: column; align-items: center; }
    .af__rail::after {
      content: ''; position: absolute; top: 28px; bottom: -6px; width: 2px; background: var(--border);
    }
    .af__item--last .af__rail::after { display: none; }
    .af__icon {
      position: relative; z-index: 1; width: 28px; height: 28px; border-radius: 50%;
      background: var(--surface-sunken); color: var(--text-muted); border: 1px solid var(--border);
      display: flex; align-items: center; justify-content: center; font-weight: 600; font-size: 0.72rem;
    }
    .af__body { flex: 1; padding-bottom: var(--s-4); padding-top: 3px; min-width: 0; }
    .af__text { display: block; font-size: var(--fs-body); color: var(--text); }
    .af__text strong { color: var(--text-strong); font-weight: 600; }
    .af__target { color: var(--brand-text); font-weight: 500; }
    .af__time { font-size: var(--fs-caption); color: var(--text-subtle); }
  `]
})
export class GmsActivityFeed {
  readonly items = input<ActivityItem[]>([]);

  showHeader(index: number): boolean {
    const items = this.items();
    return index === 0 || items[index].group !== items[index - 1].group;
  }
}
