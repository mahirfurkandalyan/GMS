import { Component, computed, inject, input } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { GmsIcon, IconName } from '../../icon/icon';
import { LanguageService } from '../../../core/language.service';

export type PageStateVariant =
  | 'loading'
  | 'empty'
  | 'error'
  | 'offline'
  | 'denied'
  | 'success';

interface StatePreset {
  icon: IconName;
  titleKey: string;
  textKey: string;
}

const PRESETS: Record<Exclude<PageStateVariant, 'loading'>, StatePreset> = {
  empty: { icon: 'inbox', titleKey: 'state.empty.title', textKey: 'state.empty.text' },
  error: { icon: 'close', titleKey: 'state.error.title', textKey: 'state.error.text' },
  offline: { icon: 'server', titleKey: 'state.offline.title', textKey: 'state.offline.text' },
  denied: { icon: 'lock', titleKey: 'state.denied.title', textKey: 'state.denied.text' },
  success: { icon: 'check', titleKey: 'state.success.title', textKey: 'state.success.text' }
};

/**
 * Reusable page-state block covering every non-content state a screen can show.
 * `<gms-state variant="offline"><button gmsButton (click)="retry()">Tekrar dene</button></gms-state>`
 */
@Component({
  selector: 'gms-state',
  standalone: true,
  imports: [GmsIcon],
  template: `
    @if (variant() === 'loading') {
      <div class="state-skel">
        @for (i of [1,2,3,4]; track i) {
          <div class="state-skel__row">
            <span class="skeleton skeleton--line" style="width:32%"></span>
            <span class="skeleton skeleton--line" style="width:22%"></span>
            <span class="skeleton skeleton--line" style="width:16%"></span>
          </div>
        }
      </div>
    } @else {
      <div class="empty-state" [class]="'empty-state--tone-' + variant()">
        <span class="empty-state__icon"><gms-icon [name]="icon()" [size]="22" /></span>
        <span class="empty-state__title">{{ resolvedTitle() }}</span>
        <span class="empty-state__text">{{ resolvedText() }}</span>
        @if (steps().length) {
          <ol class="state-steps">
            @for (step of steps(); track step) {
              <li class="state-steps__item"><span class="state-steps__dot"></span>{{ step }}</li>
            }
          </ol>
        }
        <ng-content></ng-content>
      </div>
    }
  `,
  styles: [`
    :host { display: block; }
    .state-skel { display: flex; flex-direction: column; }
    .state-skel__row { display: flex; gap: var(--s-4); align-items: center; padding: var(--s-3) var(--s-2); border-bottom: 1px solid var(--border); }
    .state-skel__row:last-child { border-bottom: 0; }
    .state-steps { list-style: none; margin: var(--s-2) 0 0; padding: 0; display: flex; flex-direction: column; gap: 6px; text-align: left; max-width: 400px; }
    .state-steps__item { display: flex; align-items: center; gap: var(--s-2); font-size: var(--fs-sm); color: var(--text); }
    .state-steps__dot { width: 6px; height: 6px; border-radius: 50%; background: var(--brand); flex-shrink: 0; }
    .empty-state--tone-error .empty-state__icon { background: var(--danger-bg); color: var(--danger); border-color: transparent; }
    .empty-state--tone-offline .empty-state__icon { background: var(--warning-bg); color: var(--warning); border-color: transparent; }
    .empty-state--tone-denied .empty-state__icon { background: var(--neutral-bg); color: var(--neutral); border-color: transparent; }
    .empty-state--tone-success .empty-state__icon { background: var(--success-bg); color: var(--success); border-color: transparent; }
  `]
})
export class GmsState {
  private readonly transloco = inject(TranslocoService);
  private readonly language = inject(LanguageService);

  readonly variant = input<PageStateVariant>('empty');
  readonly title = input<string | null>(null);
  readonly text = input<string | null>(null);
  readonly iconOverride = input<IconName | null>(null);
  /** Educational "next steps" shown under the message. */
  readonly steps = input<string[]>([]);

  private readonly preset = computed<StatePreset>(() => {
    const v = this.variant();
    return v === 'loading' ? PRESETS.empty : PRESETS[v];
  });

  protected readonly icon = computed<IconName>(() => this.iconOverride() ?? this.preset().icon);
  protected readonly resolvedTitle = computed(() => {
    this.language.current();
    return this.title() ?? this.transloco.translate(this.preset().titleKey);
  });
  protected readonly resolvedText = computed(() => {
    this.language.current();
    return this.text() ?? this.transloco.translate(this.preset().textKey);
  });
}
