import { Component, input } from '@angular/core';
import { GmsIcon, IconName } from '../../icon/icon';
import { BadgeTone } from '../badge/badge';

export interface TimelineItem {
  title: string;
  time: string;
  description?: string;
  tone?: BadgeTone;
  icon?: IconName;
}

/**
 * Reusable enterprise timeline (releases / changes / execution / audit).
 * `<gms-timeline [items]="steps" />`
 */
@Component({
  selector: 'gms-timeline',
  standalone: true,
  imports: [GmsIcon],
  template: `
    <ol class="tl">
      @for (item of items(); track $index; let last = $last) {
        <li class="tl__item" [class.tl__item--last]="last">
          <span class="tl__marker" [class]="'tl__marker--' + (item.tone ?? 'neutral')">
            @if (item.icon) { <gms-icon [name]="item.icon" [size]="13" /> }
          </span>
          <div class="tl__content">
            <div class="tl__head">
              <span class="tl__title">{{ item.title }}</span>
              <span class="tl__time">{{ item.time }}</span>
            </div>
            @if (item.description) { <p class="tl__desc">{{ item.description }}</p> }
          </div>
        </li>
      }
    </ol>
  `,
  styles: [`
    :host { display: block; }
    .tl { list-style: none; margin: 0; padding: 0; }
    .tl__item { position: relative; display: flex; gap: var(--s-3); padding-bottom: var(--s-5); }
    .tl__item::before {
      content: ''; position: absolute; left: 10px; top: 22px; bottom: 0;
      width: 2px; background: var(--border);
    }
    .tl__item--last::before { display: none; }
    .tl__marker {
      position: relative; z-index: 1; flex-shrink: 0;
      width: 22px; height: 22px; border-radius: 50%;
      display: flex; align-items: center; justify-content: center;
      background: var(--surface); border: 2px solid var(--border-strong); color: var(--text-muted);
    }
    .tl__marker--info { border-color: var(--info); color: var(--info); }
    .tl__marker--success { border-color: var(--success); color: var(--success); }
    .tl__marker--warning { border-color: var(--warning); color: var(--warning); }
    .tl__marker--danger { border-color: var(--danger); color: var(--danger); }
    .tl__content { flex: 1; padding-top: 1px; }
    .tl__head { display: flex; align-items: baseline; justify-content: space-between; gap: var(--s-3); }
    .tl__title { font-weight: 600; font-size: var(--fs-body); color: var(--text-strong); }
    .tl__time { font-size: var(--fs-caption); color: var(--text-subtle); white-space: nowrap; }
    .tl__desc { margin: 2px 0 0; font-size: var(--fs-sm); color: var(--text-muted); }
  `]
})
export class GmsTimeline {
  readonly items = input<TimelineItem[]>([]);
}
