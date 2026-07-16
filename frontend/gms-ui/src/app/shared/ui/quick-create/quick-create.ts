import { Component, computed, inject, input, output } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { GmsIcon, IconName } from '../../icon/icon';
import { GmsMenu, MenuItem } from '../menu/menu';
import { LanguageService } from '../../../core/language.service';

export interface QuickCreateAction {
  id: string;
  labelKey: string;
  icon: IconName;
  route?: string;
  disabled?: boolean;
}

const DEFAULT_ACTIONS: QuickCreateAction[] = [
  { id: 'release', labelKey: 'quickCreate.release', icon: 'release', route: '/releases' },
  { id: 'change', labelKey: 'quickCreate.change', icon: 'change', disabled: true },
  { id: 'project', labelKey: 'quickCreate.project', icon: 'folder', disabled: true },
  { id: 'document', labelKey: 'quickCreate.document', icon: 'document', disabled: true },
  { id: 'template', labelKey: 'quickCreate.template', icon: 'document', disabled: true }
];

/** Reusable quick-create launcher (topbar / workspace header). Future-ready via `actions` config. */
@Component({
  selector: 'gms-quick-create',
  standalone: true,
  imports: [GmsIcon, GmsMenu, TranslocoPipe],
  template: `
    <gms-menu [items]="menuItems()" [defaultTrigger]="false" (select)="run($event)">
      <button trigger type="button" class="gms-btn gms-btn--primary" [class.gms-btn--sm]="size() === 'sm'">
        <gms-icon name="plus" [size]="16" />
        <span class="qc__label">{{ 'quickCreate.trigger' | transloco }}</span>
        <gms-icon name="chevron-down" [size]="14" />
      </button>
    </gms-menu>
  `,
  styles: [`:host { display: inline-flex; } @media (max-width: 640px) { .qc__label { display: none; } }`]
})
export class GmsQuickCreate {
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  private readonly language = inject(LanguageService);

  readonly actions = input<QuickCreateAction[]>(DEFAULT_ACTIONS);
  readonly size = input<'sm' | 'md'>('md');
  readonly select = output<string>();

  protected readonly menuItems = computed<MenuItem[]>(() => {
    this.language.current();
    return this.actions().map((a) => ({
      label: this.transloco.translate(a.labelKey),
      value: a.id,
      icon: a.icon,
      disabled: a.disabled
    }));
  });

  run(id: string): void {
    const action = this.actions().find((a) => a.id === id);
    this.select.emit(id);
    if (action?.route) {
      this.router.navigateByUrl(action.route);
    }
  }
}
