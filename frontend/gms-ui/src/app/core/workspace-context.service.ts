import { Injectable, computed, signal } from '@angular/core';
import { Crumb } from '../shared/ui/breadcrumbs/breadcrumbs';

export interface ContextRef {
  label: string;
  route?: string;
}

export interface WorkspaceContext {
  customer?: ContextRef | null;
  project?: ContextRef | null;
  environment?: ContextRef | null;
  release?: ContextRef | null;
  change?: ContextRef | null;
}

/**
 * Ambient workspace context — the answer to "where am I / what am I working on".
 * Pages set the current object chain; headers, breadcrumbs and relationship
 * strips read from it so the user is never lost. No backend; purely UI state.
 */
@Injectable({ providedIn: 'root' })
export class WorkspaceContextService {
  private readonly ctx = signal<WorkspaceContext>({});
  readonly context = this.ctx.asReadonly();

  set(context: WorkspaceContext): void {
    this.ctx.set(context);
  }

  patch(partial: WorkspaceContext): void {
    this.ctx.update((c) => ({ ...c, ...partial }));
  }

  clear(): void {
    this.ctx.set({});
  }

  /** Breadcrumb trail derived from the active context chain. */
  readonly crumbs = computed<Crumb[]>(() => {
    const c = this.ctx();
    const trail: Crumb[] = [];
    for (const ref of [c.customer, c.project, c.environment, c.release, c.change]) {
      if (ref) trail.push({ label: ref.label, route: ref.route });
    }
    return trail;
  });
}
