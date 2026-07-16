import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { GmsIcon } from '../../icon/icon';

export interface Crumb {
  label: string;
  route?: string;
}

/** `<gms-breadcrumbs [items]="[{label:'Organizasyon'},{label:'Çalışanlar',route:'/employees'}]" />` */
@Component({
  selector: 'gms-breadcrumbs',
  standalone: true,
  imports: [RouterLink, GmsIcon],
  template: `
    <nav class="crumbs" aria-label="breadcrumb">
      @for (item of items(); track item.label; let last = $last) {
        @if (item.route && !last) {
          <a class="crumbs__link" [routerLink]="item.route">{{ item.label }}</a>
        } @else {
          <span class="crumbs__current" [attr.aria-current]="last ? 'page' : null">{{ item.label }}</span>
        }
        @if (!last) { <span class="crumbs__sep"><gms-icon name="chevron-right" [size]="13" /></span> }
      }
    </nav>
  `,
  styles: [`
    .crumbs { display: flex; align-items: center; gap: 6px; font-size: var(--fs-sm); }
    .crumbs__link { color: var(--text-muted); text-decoration: none; }
    .crumbs__link:hover { color: var(--brand-text); }
    .crumbs__current { color: var(--text); font-weight: 500; }
    .crumbs__sep { color: var(--text-subtle); display: flex; }
  `]
})
export class GmsBreadcrumbs {
  readonly items = input<Crumb[]>([]);
}
