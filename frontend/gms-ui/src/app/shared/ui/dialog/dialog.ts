import { Component, Injectable, inject, input, output, signal } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { GmsIcon } from '../../icon/icon';
import { GmsButton } from '../button/button';

/**
 * Declarative modal. `<gms-modal [open]="open()" title="Başlık" (closed)="open.set(false)">
 *   body… <div footer>…</div></gms-modal>`
 */
@Component({
  selector: 'gms-modal',
  standalone: true,
  imports: [GmsIcon, TranslocoPipe],
  host: { '(document:keydown.escape)': 'onEsc()' },
  template: `
    @if (open()) {
      <div class="ovl" (click)="backdrop()">
        <div class="dlg" [class]="'dlg--' + size()" role="dialog" aria-modal="true" (click)="$event.stopPropagation()">
          <div class="dlg__head">
            <h2 class="dlg__title">{{ title() }}</h2>
            <button type="button" class="dlg__close" (click)="closed.emit()" [attr.aria-label]="'common.close' | transloco"><gms-icon name="close" [size]="18" /></button>
          </div>
          <div class="dlg__body"><ng-content></ng-content></div>
          <div class="dlg__foot"><ng-content select="[footer]"></ng-content></div>
        </div>
      </div>
    }
  `,
  styles: [`
    .ovl {
      position: fixed; inset: 0; z-index: var(--z-overlay); background: var(--overlay);
      display: flex; align-items: center; justify-content: center; padding: var(--s-4);
      animation: ovl-in var(--motion-fast) var(--ease);
    }
    .dlg {
      background: var(--surface); border: 1px solid var(--border);
      border-radius: var(--r-lg); box-shadow: var(--shadow-lg);
      width: 100%; max-height: 88vh; display: flex; flex-direction: column;
      animation: dlg-in var(--motion) var(--ease-out);
    }
    .dlg--sm { max-width: 420px; } .dlg--md { max-width: 560px; } .dlg--lg { max-width: 760px; }
    .dlg__head { display: flex; align-items: center; justify-content: space-between; padding: var(--s-4) var(--s-5); border-bottom: 1px solid var(--border); }
    .dlg__title { font-size: var(--fs-h2); font-weight: 600; color: var(--text-strong); }
    .dlg__close { border: 0; background: transparent; color: var(--text-muted); cursor: pointer; padding: 4px; border-radius: var(--r-sm); display: flex; }
    .dlg__close:hover { background: var(--surface-hover); color: var(--text-strong); }
    .dlg__body { padding: var(--s-5); overflow-y: auto; }
    .dlg__foot:not(:empty) { padding: var(--s-4) var(--s-5); border-top: 1px solid var(--border); display: flex; justify-content: flex-end; gap: var(--s-2); }
    @keyframes ovl-in { from { opacity: 0; } to { opacity: 1; } }
    @keyframes dlg-in { from { opacity: 0; transform: translateY(8px) scale(0.98); } to { opacity: 1; transform: none; } }
  `]
})
export class GmsModal {
  readonly open = input(false);
  readonly title = input('');
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly closeOnBackdrop = input(true);
  readonly closed = output<void>();

  backdrop(): void { if (this.closeOnBackdrop()) this.closed.emit(); }
  onEsc(): void { if (this.open()) this.closed.emit(); }
}

/** Declarative right/left drawer. */
@Component({
  selector: 'gms-drawer',
  standalone: true,
  imports: [GmsIcon, TranslocoPipe],
  host: { '(document:keydown.escape)': 'onEsc()' },
  template: `
    @if (open()) {
      <div class="ovl" (click)="closed.emit()">
        <aside class="drw" [class]="'drw--' + side()" [style.width]="width()" role="dialog" aria-modal="true" (click)="$event.stopPropagation()">
          <div class="drw__head">
            <h2 class="drw__title">{{ title() }}</h2>
            <button type="button" class="drw__close" (click)="closed.emit()" [attr.aria-label]="'common.close' | transloco"><gms-icon name="close" [size]="18" /></button>
          </div>
          <div class="drw__body"><ng-content></ng-content></div>
        </aside>
      </div>
    }
  `,
  styles: [`
    .ovl { position: fixed; inset: 0; z-index: var(--z-overlay); background: var(--overlay); animation: ovl-in var(--motion-fast) var(--ease); }
    .drw {
      position: absolute; top: 0; bottom: 0; background: var(--surface);
      display: flex; flex-direction: column; box-shadow: var(--shadow-lg); max-width: 92vw;
    }
    .drw--right { right: 0; border-left: 1px solid var(--border); animation: drw-r var(--motion) var(--ease-out); }
    .drw--left { left: 0; border-right: 1px solid var(--border); animation: drw-l var(--motion) var(--ease-out); }
    .drw__head { display: flex; align-items: center; justify-content: space-between; padding: var(--s-4) var(--s-5); border-bottom: 1px solid var(--border); }
    .drw__title { font-size: var(--fs-h2); font-weight: 600; color: var(--text-strong); }
    .drw__close { border: 0; background: transparent; color: var(--text-muted); cursor: pointer; padding: 4px; border-radius: var(--r-sm); display: flex; }
    .drw__close:hover { background: var(--surface-hover); }
    .drw__body { padding: var(--s-5); overflow-y: auto; flex: 1; }
    @keyframes ovl-in { from { opacity: 0; } to { opacity: 1; } }
    @keyframes drw-r { from { transform: translateX(100%); } to { transform: none; } }
    @keyframes drw-l { from { transform: translateX(-100%); } to { transform: none; } }
  `]
})
export class GmsDrawer {
  readonly open = input(false);
  readonly title = input('');
  readonly side = input<'right' | 'left'>('right');
  readonly width = input('440px');
  readonly closed = output<void>();

  onEsc(): void { if (this.open()) this.closed.emit(); }
}

export type ConfirmVariant = 'info' | 'success' | 'warning' | 'danger';

export interface ConfirmOptions {
  title?: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  destructive?: boolean;
  variant?: ConfirmVariant;
}

interface ConfirmState extends ConfirmOptions {
  resolve: (v: boolean) => void;
}

/** Programmatic confirmation: `await confirm.ask({ message: '…', destructive: true })` */
@Injectable({ providedIn: 'root' })
export class ConfirmService {
  readonly state = signal<ConfirmState | null>(null);

  ask(options: ConfirmOptions): Promise<boolean> {
    return new Promise((resolve) => this.state.set({ ...options, resolve }));
  }

  resolve(value: boolean): void {
    this.state()?.resolve(value);
    this.state.set(null);
  }
}

/** Confirm host — mount once near app root. Supports success/warning/danger/info variants. */
@Component({
  selector: 'gms-confirm-host',
  standalone: true,
  imports: [GmsModal, GmsButton, GmsIcon],
  template: `
    @if (state(); as s) {
      <gms-modal [open]="true" size="sm" [title]="s.title ?? defaultTitle()" (closed)="cancel()">
        <div class="confirm">
          <span class="confirm__icon" [class]="'confirm__icon--' + variant(s)">
            <gms-icon [name]="icon(s)" [size]="20" />
          </span>
          <p class="confirm__msg">{{ s.message }}</p>
        </div>
        <div footer>
          <button gmsButton variant="ghost" (click)="cancel()">{{ s.cancelText ?? defaultCancelText() }}</button>
          <button gmsButton [variant]="danger(s) ? 'destructive' : 'primary'" (click)="accept()">
            {{ s.confirmText ?? defaultConfirmText() }}
          </button>
        </div>
      </gms-modal>
    }
  `,
  styles: [`
    .confirm { display: flex; align-items: flex-start; gap: var(--s-3); }
    .confirm__icon { flex-shrink: 0; width: 40px; height: 40px; border-radius: var(--r-md); display: flex; align-items: center; justify-content: center; }
    .confirm__icon--info { background: var(--info-bg); color: var(--info); }
    .confirm__icon--success { background: var(--success-bg); color: var(--success); }
    .confirm__icon--warning { background: var(--warning-bg); color: var(--warning); }
    .confirm__icon--danger { background: var(--danger-bg); color: var(--danger); }
    .confirm__msg { margin: 0; padding-top: 8px; color: var(--text); font-size: var(--fs-body); line-height: var(--lh-normal); }
  `]
})
export class GmsConfirmHost {
  private readonly service = inject(ConfirmService);
  private readonly transloco = inject(TranslocoService);
  protected readonly state = this.service.state;

  protected defaultTitle(): string { return this.transloco.translate('common.confirmTitle'); }
  protected defaultCancelText(): string { return this.transloco.translate('common.cancel'); }
  protected defaultConfirmText(): string { return this.transloco.translate('common.confirmAction'); }

  variant(s: ConfirmOptions): ConfirmVariant {
    return s.variant ?? (s.destructive ? 'danger' : 'info');
  }
  danger(s: ConfirmOptions): boolean {
    return this.variant(s) === 'danger';
  }
  icon(s: ConfirmOptions) {
    switch (this.variant(s)) {
      case 'success': return 'check' as const;
      case 'warning': return 'clock' as const;
      case 'danger': return 'close' as const;
      default: return 'inbox' as const;
    }
  }

  accept(): void { this.service.resolve(true); }
  cancel(): void { this.service.resolve(false); }
}
