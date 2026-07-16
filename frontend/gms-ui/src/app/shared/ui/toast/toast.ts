import { Component, Injectable, computed, inject, signal } from '@angular/core';
import { GmsIcon, IconName } from '../../icon/icon';

export type ToastTone = 'info' | 'success' | 'warning' | 'danger';

export interface ToastAction {
  label: string;
  run: () => void;
}

export interface Toast {
  id: number;
  tone: ToastTone;
  title?: string;
  text: string;
  action?: ToastAction;
}

export interface ToastOptions {
  title?: string;
  timeout?: number;
  action?: ToastAction;
}

/**
 * Global toast service. Inject anywhere:
 * `inject(ToastService).success('Yayın oluşturuldu')`
 * Undo pattern: `toast.undo('Kayıt silindi', () => restore())`
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly items = signal<Toast[]>([]);
  private seq = 0;
  readonly toasts = this.items.asReadonly();

  show(text: string, tone: ToastTone = 'info', opts: ToastOptions = {}): number {
    const id = ++this.seq;
    const timeout = opts.timeout ?? 4200;
    this.items.update((list) => [...list, { id, tone, text, title: opts.title, action: opts.action }]);
    if (timeout > 0) {
      setTimeout(() => this.dismiss(id), timeout);
    }
    return id;
  }

  success(text: string, title?: string) { return this.show(text, 'success', { title }); }
  error(text: string, title?: string) { return this.show(text, 'danger', { title }); }
  warning(text: string, title?: string) { return this.show(text, 'warning', { title }); }
  info(text: string, title?: string) { return this.show(text, 'info', { title }); }

  /** Show a toast with an action button (e.g. "Geri Al"). */
  action(text: string, action: ToastAction, tone: ToastTone = 'info', title?: string) {
    return this.show(text, tone, { title, action, timeout: 7000 });
  }

  /** Convenience: undoable action feedback. */
  undo(text: string, onUndo: () => void, title?: string) {
    return this.action(text, { label: 'Geri Al', run: onUndo }, 'success', title);
  }

  runAction(id: number): void {
    const toast = this.items().find((t) => t.id === id);
    toast?.action?.run();
    this.dismiss(id);
  }

  dismiss(id: number): void {
    this.items.update((list) => list.filter((t) => t.id !== id));
  }
}

/** Toast host — mount once near the app root. */
@Component({
  selector: 'gms-toast-host',
  standalone: true,
  imports: [GmsIcon],
  template: `
    <div class="toast-host" aria-live="polite" aria-atomic="true">
      @for (t of toasts(); track t.id) {
        <div class="toast" [class]="'toast--' + t.tone" role="status">
          <span class="toast__icon"><gms-icon [name]="iconFor(t.tone)" [size]="18" /></span>
          <span class="toast__body">
            @if (t.title) { <span class="toast__title">{{ t.title }}</span> }
            <span class="toast__text">{{ t.text }}</span>
          </span>
          @if (t.action) {
            <button type="button" class="toast__action" (click)="runAction(t.id)">{{ t.action.label }}</button>
          }
          <button type="button" class="toast__close" (click)="dismiss(t.id)" aria-label="Kapat">
            <gms-icon name="close" [size]="15" />
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .toast-host {
      position: fixed; top: 16px; right: 16px; z-index: var(--z-toast);
      display: flex; flex-direction: column; gap: var(--s-2); max-width: 380px; width: calc(100vw - 32px);
      pointer-events: none;
    }
    .toast {
      pointer-events: auto;
      display: flex; align-items: flex-start; gap: var(--s-3);
      padding: var(--s-3) var(--s-4);
      background: var(--surface); border: 1px solid var(--border);
      border-left: 3px solid var(--border-strong);
      border-radius: var(--r-md); box-shadow: var(--shadow-lg);
      animation: toast-in var(--motion) var(--ease-out);
    }
    .toast__icon { flex-shrink: 0; margin-top: 1px; }
    .toast__body { flex: 1; display: flex; flex-direction: column; gap: 2px; }
    .toast__title { font-weight: 600; font-size: var(--fs-body); color: var(--text-strong); }
    .toast__text { font-size: var(--fs-sm); color: var(--text); }
    .toast__action { border: 0; background: transparent; color: var(--brand-text); font: inherit; font-size: var(--fs-sm); font-weight: 600; cursor: pointer; padding: 2px 6px; border-radius: var(--r-xs); white-space: nowrap; align-self: center; }
    .toast__action:hover { background: var(--brand-subtle); }
    .toast__close { border: 0; background: transparent; color: var(--text-subtle); cursor: pointer; padding: 2px; border-radius: var(--r-xs); display: flex; }
    .toast__close:hover { background: var(--surface-hover); color: var(--text-strong); }
    .toast--success { border-left-color: var(--success); } .toast--success .toast__icon { color: var(--success); }
    .toast--danger { border-left-color: var(--danger); } .toast--danger .toast__icon { color: var(--danger); }
    .toast--warning { border-left-color: var(--warning); } .toast--warning .toast__icon { color: var(--warning); }
    .toast--info { border-left-color: var(--info); } .toast--info .toast__icon { color: var(--info); }
    @keyframes toast-in { from { opacity: 0; transform: translateX(12px); } to { opacity: 1; transform: none; } }
  `]
})
export class GmsToastHost {
  private readonly service = inject(ToastService);
  protected readonly toasts = this.service.toasts;

  iconFor(tone: ToastTone): IconName {
    switch (tone) {
      case 'success': return 'check';
      case 'danger': return 'close';
      case 'warning': return 'clock';
      default: return 'bell';
    }
  }

  runAction(id: number): void {
    this.service.runAction(id);
  }

  dismiss(id: number): void {
    this.service.dismiss(id);
  }
}
