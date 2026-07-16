import { Directive, computed, input } from '@angular/core';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'destructive';
export type ButtonSize = 'sm' | 'md' | 'lg';

/**
 * GMS button — reusable across `<button>` and `<a routerLink>`.
 * Usage: `<button gmsButton variant="primary">Kaydet</button>`
 *        `<a gmsButton variant="ghost" routerLink="/x">Aç</a>`
 *        `<button gmsButton variant="ghost" [iconOnly]="true"><gms-icon .../></button>`
 */
@Directive({
  selector: '[gmsButton]',
  standalone: true,
  host: {
    '[class]': 'classes()',
    '[attr.aria-busy]': 'loading() ? true : null',
    '[attr.disabled]': 'isNativelyDisabled()'
  }
})
export class GmsButton {
  readonly variant = input<ButtonVariant>('secondary');
  readonly size = input<ButtonSize>('md');
  readonly loading = input(false);
  readonly block = input(false);
  readonly iconOnly = input(false);
  /** Only applies to native <button>; ignored on <a>. */
  readonly disabled = input(false);

  protected readonly isNativelyDisabled = computed(() =>
    this.disabled() ? true : null
  );

  protected readonly classes = computed(() => {
    const c = ['gms-btn', `gms-btn--${this.variant()}`];
    if (this.size() !== 'md') c.push(`gms-btn--${this.size()}`);
    if (this.iconOnly()) c.push('gms-btn--icon');
    if (this.block()) c.push('gms-btn--block');
    if (this.loading()) c.push('is-loading');
    return c.join(' ');
  });
}
