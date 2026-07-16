import { Component, input } from '@angular/core';

/**
 * Grouped form section with title/description and a responsive control grid.
 * `<gms-form-section title="Genel" description="…"><gms-field>…</gms-field></gms-form-section>`
 */
@Component({
  selector: 'gms-form-section',
  standalone: true,
  template: `
    <section class="fs">
      @if (title() || description()) {
        <div class="fs__head">
          @if (title()) { <h3 class="fs__title">{{ title() }}</h3> }
          @if (description()) { <p class="fs__desc">{{ description() }}</p> }
        </div>
      }
      <div class="fs__grid" [style.grid-template-columns]="columns() === 1 ? '1fr' : null">
        <ng-content></ng-content>
      </div>
    </section>
  `,
  styles: [`
    :host { display: block; }
    .fs { display: flex; flex-direction: column; gap: var(--s-4); }
    .fs__head { display: flex; flex-direction: column; gap: 2px; }
    .fs__title { font-size: var(--fs-h3); font-weight: 600; color: var(--text-strong); }
    .fs__desc { margin: 0; font-size: var(--fs-sm); color: var(--text-muted); }
    .fs__grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: var(--s-4); }
  `]
})
export class GmsFormSection {
  readonly title = input<string | null>(null);
  readonly description = input<string | null>(null);
  readonly columns = input<1 | 2>(2);
}
