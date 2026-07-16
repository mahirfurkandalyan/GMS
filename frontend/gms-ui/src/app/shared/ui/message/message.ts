import { Component, computed, input, output } from '@angular/core';
import { GmsIcon, IconName } from '../../icon/icon';

export type MessageTone = 'info' | 'success' | 'warning' | 'danger';

/**
 * Inline message / banner. Set `banner=true` for a full-width banner.
 * `<gms-message tone="warning" title="Dikkat">Metin</gms-message>`
 */
@Component({
  selector: 'gms-message',
  standalone: true,
  imports: [GmsIcon],
  template: `
    <div class="msg" [class]="'msg--' + tone()" [class.msg--banner]="banner()" role="status">
      <span class="msg__icon"><gms-icon [name]="icon()" [size]="18" /></span>
      <span class="msg__body">
        @if (title()) { <span class="msg__title">{{ title() }}</span> }
        <span class="msg__text"><ng-content></ng-content></span>
      </span>
      @if (dismissible()) {
        <button type="button" class="msg__close" (click)="dismissed.emit()" aria-label="Kapat">
          <gms-icon name="close" [size]="16" />
        </button>
      }
    </div>
  `,
  styles: [`
    :host { display: block; }
    .msg {
      display: flex; align-items: flex-start; gap: var(--s-3);
      padding: var(--s-3) var(--s-4);
      border: 1px solid var(--border);
      border-radius: var(--r-md);
      background: var(--surface);
      font-size: var(--fs-body);
    }
    .msg--banner { border-radius: 0; border-left: 0; border-right: 0; }
    .msg__icon { flex-shrink: 0; margin-top: 1px; }
    .msg__body { flex: 1; display: flex; flex-direction: column; gap: 2px; }
    .msg__title { font-weight: 600; color: var(--text-strong); }
    .msg__text { color: var(--text); }
    .msg__close {
      border: 0; background: transparent; color: var(--text-muted);
      cursor: pointer; padding: 2px; border-radius: var(--r-xs); display: flex;
    }
    .msg__close:hover { background: var(--surface-hover); color: var(--text-strong); }
    .msg--info { background: var(--info-bg); border-color: color-mix(in srgb, var(--info) 22%, transparent); }
    .msg--info .msg__icon { color: var(--info); }
    .msg--success { background: var(--success-bg); border-color: color-mix(in srgb, var(--success) 22%, transparent); }
    .msg--success .msg__icon { color: var(--success); }
    .msg--warning { background: var(--warning-bg); border-color: color-mix(in srgb, var(--warning) 22%, transparent); }
    .msg--warning .msg__icon { color: var(--warning); }
    .msg--danger { background: var(--danger-bg); border-color: color-mix(in srgb, var(--danger) 22%, transparent); }
    .msg--danger .msg__icon { color: var(--danger); }
  `]
})
export class GmsMessage {
  readonly tone = input<MessageTone>('info');
  readonly title = input<string | null>(null);
  readonly banner = input(false);
  readonly dismissible = input(false);
  readonly dismissed = output<void>();

  protected readonly icon = computed<IconName>(() => {
    switch (this.tone()) {
      case 'success': return 'check';
      case 'warning': return 'clock';
      case 'danger': return 'close';
      default: return 'inbox';
    }
  });
}
