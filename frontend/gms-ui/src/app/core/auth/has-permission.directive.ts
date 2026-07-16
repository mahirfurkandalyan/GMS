import { Directive, EmbeddedViewRef, TemplateRef, ViewContainerRef, effect, inject, input } from '@angular/core';
import { AuthStateService } from './auth-state.service';

/**
 * Structural directives for permission/role-based UI convenience. They HIDE elements the user
 * cannot use — this is a UX aid only; the backend remains the security authority. Centralizes the
 * check so templates never compare role/permission strings by hand.
 *
 *   <button *gmsHasPermission="'release.create'">Yeni Yayın</button>
 *   <a *gmsHasAnyPermission="['audit.read','report.read']">Raporlar</a>
 *   <div *gmsHasRole="'Admin'">…</div>
 */

abstract class ToggleDirective {
  protected readonly tpl = inject<TemplateRef<unknown>>(TemplateRef);
  protected readonly vcr = inject(ViewContainerRef);
  protected readonly auth = inject(AuthStateService);
  private view: EmbeddedViewRef<unknown> | null = null;

  protected toggle(show: boolean): void {
    if (show && !this.view) {
      this.view = this.vcr.createEmbeddedView(this.tpl);
    } else if (!show && this.view) {
      this.vcr.clear();
      this.view = null;
    }
  }
}

@Directive({ selector: '[gmsHasPermission]' })
export class HasPermissionDirective extends ToggleDirective {
  readonly gmsHasPermission = input.required<string>();
  constructor() {
    super();
    effect(() => this.toggle(this.auth.hasPermission(this.gmsHasPermission())));
  }
}

@Directive({ selector: '[gmsHasAnyPermission]' })
export class HasAnyPermissionDirective extends ToggleDirective {
  readonly gmsHasAnyPermission = input.required<string[]>();
  constructor() {
    super();
    effect(() => this.toggle(this.auth.hasAnyPermission(this.gmsHasAnyPermission())));
  }
}

@Directive({ selector: '[gmsHasRole]' })
export class HasRoleDirective extends ToggleDirective {
  readonly gmsHasRole = input.required<string | string[]>();
  constructor() {
    super();
    effect(() => {
      const value = this.gmsHasRole();
      this.toggle(this.auth.hasAnyRole(Array.isArray(value) ? value : [value]));
    });
  }
}
