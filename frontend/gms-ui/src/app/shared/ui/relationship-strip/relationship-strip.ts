import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { GmsIcon, IconName } from '../../icon/icon';

export interface RelationNode {
  label: string;
  type: string;
  route?: string;
  current?: boolean;
  icon?: IconName;
}

/**
 * Object-relationship strip — visualizes the object chain and lets users move
 * between related objects: Müşteri → Proje → Ortam → Yayın → Değişiklik → Yürütme.
 * `<gms-relationship-strip [nodes]="chain" />`
 */
@Component({
  selector: 'gms-relationship-strip',
  standalone: true,
  imports: [RouterLink, GmsIcon],
  template: `
    <div class="rel">
      @for (node of nodes(); track node.label; let last = $last) {
        @if (node.route && !node.current) {
          <a class="rel__node" [routerLink]="node.route">
            @if (node.icon) { <gms-icon [name]="node.icon" [size]="14" /> }
            <span class="rel__meta">
              <span class="rel__type">{{ node.type }}</span>
              <span class="rel__label">{{ node.label }}</span>
            </span>
          </a>
        } @else {
          <span class="rel__node" [class.rel__node--current]="node.current">
            @if (node.icon) { <gms-icon [name]="node.icon" [size]="14" /> }
            <span class="rel__meta">
              <span class="rel__type">{{ node.type }}</span>
              <span class="rel__label">{{ node.label }}</span>
            </span>
          </span>
        }
        @if (!last) { <span class="rel__sep"><gms-icon name="chevron-right" [size]="14" /></span> }
      }
    </div>
  `,
  styles: [`
    :host { display: block; }
    .rel { display: flex; align-items: center; gap: 4px; overflow-x: auto; padding: 2px; scrollbar-width: none; }
    .rel::-webkit-scrollbar { display: none; }
    .rel__node {
      display: inline-flex; align-items: center; gap: var(--s-2); flex-shrink: 0;
      padding: 6px 10px; border-radius: var(--r-sm); text-decoration: none;
      border: 1px solid var(--border); background: var(--surface); color: var(--text);
      transition: background var(--motion-fast) var(--ease), border-color var(--motion-fast) var(--ease);
    }
    a.rel__node:hover { background: var(--surface-hover); border-color: var(--border-strong); }
    .rel__node gms-icon { color: var(--text-subtle); }
    .rel__node--current { background: var(--brand-subtle); border-color: transparent; }
    .rel__node--current .rel__label { color: var(--brand-text); }
    .rel__node--current gms-icon { color: var(--brand); }
    .rel__meta { display: flex; flex-direction: column; line-height: 1.15; }
    .rel__type { font-size: 0.62rem; text-transform: uppercase; letter-spacing: 0.05em; color: var(--text-subtle); font-weight: 600; }
    .rel__label { font-size: var(--fs-sm); font-weight: 600; color: var(--text-strong); white-space: nowrap; }
    .rel__sep { color: var(--text-subtle); display: flex; flex-shrink: 0; }
  `]
})
export class GmsRelationshipStrip {
  readonly nodes = input<RelationNode[]>([]);
}
