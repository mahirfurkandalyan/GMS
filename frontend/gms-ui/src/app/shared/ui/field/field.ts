import { Directive, Component, computed, input } from '@angular/core';

/**
 * Styling directive for native controls (input/select/textarea).
 * `<input gmsInput [invalid]="hasError" />`
 */
@Directive({
  selector: '[gmsInput]',
  standalone: true,
  host: {
    class: 'gms-input',
    '[attr.aria-invalid]': 'invalid() ? true : null'
  }
})
export class GmsInput {
  readonly invalid = input(false);
}

/**
 * Field wrapper: label + control + helper/error.
 * `<gms-field label="Ad" hint="Zorunlu" [error]="err"><input gmsInput ...></gms-field>`
 */
@Component({
  selector: 'gms-field',
  standalone: true,
  template: `
    <label class="field">
      @if (label()) {
        <span class="field__label">{{ label() }}@if (required()) { <span class="field__req">*</span> }</span>
      }
      <ng-content></ng-content>
      @if (error()) {
        <span class="field__msg field__msg--error">{{ error() }}</span>
      } @else if (hint()) {
        <span class="field__msg">{{ hint() }}</span>
      }
    </label>
  `,
  styles: [`
    :host { display: block; }
    .field { display: flex; flex-direction: column; gap: 6px; }
    .field__label { font-size: var(--fs-sm); font-weight: 500; color: var(--text); }
    .field__req { color: var(--danger); margin-left: 2px; }
    .field__msg { font-size: var(--fs-caption); color: var(--text-muted); }
    .field__msg--error { color: var(--danger); }
  `]
})
export class GmsField {
  readonly label = input<string | null>(null);
  readonly hint = input<string | null>(null);
  readonly error = input<string | null>(null);
  readonly required = input(false);

  protected readonly hasError = computed(() => !!this.error());
}
