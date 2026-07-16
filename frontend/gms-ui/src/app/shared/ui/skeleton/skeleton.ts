import { Component, computed, input } from '@angular/core';

export type SkeletonVariant = 'line' | 'title' | 'row' | 'circle' | 'block';

/** Reusable skeleton loader. `<gms-skeleton variant="line" width="60%" />` */
@Component({
  selector: 'gms-skeleton',
  standalone: true,
  template: `<span class="skeleton" [class]="variantClass()" [style.width]="width()" [style.height]="height()"></span>`,
  styles: [
    `:host { display: block; }
     .skeleton--circle { border-radius: 50%; }
     .skeleton--block { height: 100%; border-radius: var(--r-md); }`
  ]
})
export class GmsSkeleton {
  readonly variant = input<SkeletonVariant>('line');
  readonly width = input<string | null>(null);
  readonly height = input<string | null>(null);

  protected readonly variantClass = computed(() => `skeleton--${this.variant()}`);
}
