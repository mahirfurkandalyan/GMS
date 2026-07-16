import { Component, input } from '@angular/core';
import { GmsIcon, IconName } from '../../icon/icon';

/**
 * Reusable empty state.
 * `<gms-empty-state icon="release" title="Kayıt yok" text="..."><button>...</button></gms-empty-state>`
 */
@Component({
  selector: 'gms-empty-state',
  standalone: true,
  imports: [GmsIcon],
  template: `
    <div class="empty-state" [class.empty-state--inline]="inline()">
      <span class="empty-state__icon"><gms-icon [name]="icon()" [size]="inline() ? 18 : 22" /></span>
      <span class="empty-state__wrap">
        @if (title()) { <span class="empty-state__title">{{ title() }}</span> }
        @if (text()) { <span class="empty-state__text">{{ text() }}</span> }
      </span>
      <ng-content></ng-content>
    </div>
  `,
  styles: [`:host { display: block; } .empty-state__wrap { display: flex; flex-direction: column; gap: 4px; align-items: inherit; }`]
})
export class GmsEmptyState {
  readonly icon = input<IconName>('inbox');
  readonly title = input<string | null>(null);
  readonly text = input<string | null>(null);
  readonly inline = input(false);
}
