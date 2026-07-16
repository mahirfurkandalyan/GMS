import { Component, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { GmsIcon } from '../../icon/icon';
import { GmsBreadcrumbs, Crumb } from '../breadcrumbs/breadcrumbs';
import { GmsQuickCreate } from '../quick-create/quick-create';
import { BadgeTone } from '../badge/badge';

export interface WorkspaceEnv {
  label: string;
  tone: BadgeTone;
}

export interface WorkspaceContext {
  label: string;
  hint?: string;
}

/**
 * Reusable workspace header — always communicates current context:
 * breadcrumb, environment, project, release, plus favorite/share/quick-create.
 */
@Component({
  selector: 'gms-workspace-header',
  standalone: true,
  imports: [GmsIcon, GmsBreadcrumbs, GmsQuickCreate, TranslocoPipe],
  template: `
    <header class="wh">
      @if (breadcrumbs().length) { <gms-breadcrumbs [items]="breadcrumbs()" /> }

      <div class="wh__row">
        <div class="wh__titles">
          <h1 class="wh__title">{{ title() }}</h1>
          @if (subtitle()) { <p class="wh__sub">{{ subtitle() }}</p> }
        </div>

        <div class="wh__actions">
          <button type="button" class="wh__icon-btn" [class.is-active]="favorite()"
            (click)="favoriteToggle.emit()" [attr.aria-pressed]="favorite()" [title]="'common.addToFavorites' | transloco">
            <gms-icon [name]="favorite() ? 'star-filled' : 'star'" [size]="18" />
          </button>
          <button type="button" class="wh__icon-btn" (click)="share.emit()" [title]="'common.shareSoon' | transloco">
            <gms-icon name="share" [size]="17" />
          </button>
          <gms-quick-create />
        </div>
      </div>

      @if (environment() || project() || release()) {
        <div class="wh__context">
          @if (environment(); as env) {
            <span class="wh__chip">
              <span class="dot" [class]="'dot--' + dotTone(env.tone)"></span>
              <span class="wh__chip-key">{{ 'common.environment' | transloco }}</span> {{ env.label }}
            </span>
          }
          @if (project(); as p) {
            <span class="wh__chip"><gms-icon name="folder" [size]="14" /><span class="wh__chip-key">{{ 'common.project' | transloco }}</span> {{ p.label }}</span>
          }
          @if (release(); as r) {
            <span class="wh__chip"><gms-icon name="release" [size]="14" /><span class="wh__chip-key">{{ 'common.release' | transloco }}</span> {{ r.label }}</span>
          }
        </div>
      }
    </header>
  `,
  styles: [`
    :host { display: block; }
    .wh { display: flex; flex-direction: column; gap: var(--s-3); }
    .wh__row { display: flex; align-items: flex-start; justify-content: space-between; gap: var(--s-4); flex-wrap: wrap; }
    .wh__title { font-size: var(--fs-display); font-weight: 600; letter-spacing: -0.02em; color: var(--text-strong); margin: 0 0 4px; }
    .wh__sub { margin: 0; color: var(--text-muted); font-size: var(--fs-body); }
    .wh__actions { display: flex; align-items: center; gap: var(--s-2); }
    .wh__icon-btn {
      width: 38px; height: 38px; border: 1px solid var(--border-strong); background: var(--surface);
      color: var(--text-muted); border-radius: var(--r-sm); display: flex; align-items: center; justify-content: center; cursor: pointer;
      transition: all var(--motion-fast) var(--ease);
    }
    .wh__icon-btn:hover { background: var(--surface-hover); color: var(--text-strong); }
    .wh__icon-btn.is-active { color: #d9a406; border-color: color-mix(in srgb, #d9a406 40%, transparent); background: #fef9ec; }
    .wh__context { display: flex; flex-wrap: wrap; gap: var(--s-2); }
    .wh__chip {
      display: inline-flex; align-items: center; gap: 6px; height: 28px; padding: 0 10px;
      font-size: var(--fs-sm); font-weight: 500; color: var(--text);
      background: var(--surface-sunken); border: 1px solid var(--border); border-radius: var(--r-pill);
    }
    .wh__chip gms-icon { color: var(--text-subtle); }
    .wh__chip-key { color: var(--text-subtle); font-weight: 500; }
  `]
})
export class GmsWorkspaceHeader {
  readonly title = input('');
  readonly subtitle = input<string | null>(null);
  readonly breadcrumbs = input<Crumb[]>([]);
  readonly environment = input<WorkspaceEnv | null>(null);
  readonly project = input<WorkspaceContext | null>(null);
  readonly release = input<WorkspaceContext | null>(null);
  readonly favorite = input(false);
  readonly favoriteToggle = output<void>();
  readonly share = output<void>();

  dotTone(tone: BadgeTone): string {
    switch (tone) {
      case 'success': return 'green';
      case 'warning': return 'amber';
      case 'danger': return 'red';
      default: return 'gray';
    }
  }
}
