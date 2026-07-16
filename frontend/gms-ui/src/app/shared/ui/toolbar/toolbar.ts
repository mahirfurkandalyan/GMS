import { Component } from '@angular/core';

/**
 * Generic toolbar shell: `[start]` and `[end]` slots.
 * `<gms-toolbar><span start>…</span><span end>…</span></gms-toolbar>`
 */
@Component({
  selector: 'gms-toolbar',
  standalone: true,
  template: `
    <div class="tb">
      <div class="tb__start"><ng-content select="[start]"></ng-content></div>
      <div class="tb__end"><ng-content select="[end]"></ng-content></div>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .tb { display: flex; align-items: center; gap: var(--s-3); flex-wrap: wrap; }
    .tb__start { display: flex; align-items: center; gap: var(--s-2); flex: 1; min-width: 0; }
    .tb__end { display: flex; align-items: center; gap: var(--s-2); }
  `]
})
export class GmsToolbar {}
