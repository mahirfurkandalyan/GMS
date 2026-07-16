import { Injectable, signal } from '@angular/core';
import { IconName } from '../shared/icon/icon';

export type NavStatus = 'active' | 'soon';

export interface NavEntry {
  /** Transloco translation key — resolved in templates via `| transloco`. */
  labelKey: string;
  icon: IconName;
  route: string | null;
  status: NavStatus;
  /** Stable module key — future modules register/replace by this. */
  module?: string;
}

export interface NavSection {
  titleKey: string | null;
  items: NavEntry[];
}

/**
 * Central navigation registry. Drives the sidebar, mobile drawer and (future)
 * command palette from one source of truth. Future modules (EBR/MES/LIMS/QMS/…)
 * are pre-registered as "soon" so the platform's breadth is visible; a module
 * team activates its entry via `activate(module, route)` — no shell edits.
 */
@Injectable({ providedIn: 'root' })
export class NavigationService {
  private readonly store = signal<NavSection[]>([
    {
      titleKey: null,
      items: [
        { labelKey: 'nav.hub', icon: 'hub', route: '/hub', status: 'active', module: 'hub' },
        { labelKey: 'nav.dashboard', icon: 'dashboard', route: '/dashboard', status: 'active', module: 'dashboard' }
      ]
    },
    {
      titleKey: 'nav.section.workspace',
      items: [
        { labelKey: 'nav.releases', icon: 'release', route: '/releases', status: 'active', module: 'release' },
        { labelKey: 'nav.changes', icon: 'change', route: '/changes', status: 'active', module: 'change' },
        { labelKey: 'nav.approvals', icon: 'approval', route: '/approvals', status: 'active', module: 'approval' },
        { labelKey: 'nav.tasks', icon: 'approval', route: '/tasks', status: 'active', module: 'tasks' },
        { labelKey: 'nav.validation', icon: 'shield', route: '/validation', status: 'active', module: 'validation' },
        { labelKey: 'nav.executions', icon: 'execution', route: '/executions', status: 'active', module: 'execution' },
        { labelKey: 'nav.documents', icon: 'document', route: '/documents', status: 'active', module: 'document' },
        { labelKey: 'nav.assets', icon: 'server', route: '/assets', status: 'active', module: 'asset' },
        { labelKey: 'nav.training', icon: 'training', route: '/training', status: 'active', module: 'training' },
        { labelKey: 'nav.notifications', icon: 'bell', route: '/notifications', status: 'active', module: 'notifications' }
      ]
    },
    {
      titleKey: 'nav.section.organization',
      items: [
        { labelKey: 'nav.employees', icon: 'employees', route: '/employees', status: 'active', module: 'employees' },
        { labelKey: 'nav.departments', icon: 'department', route: '/organization/departments', status: 'active', module: 'departments' },
        { labelKey: 'nav.teams', icon: 'team', route: '/organization/teams', status: 'active', module: 'teams' },
        { labelKey: 'nav.orgchart', icon: 'orgchart', route: '/organization/chart', status: 'active', module: 'orgchart' },
        { labelKey: 'nav.leave', icon: 'calendar', route: '/leave', status: 'active', module: 'leave' }
      ]
    },
    {
      titleKey: 'nav.section.management',
      items: [
        { labelKey: 'nav.reports', icon: 'dashboard', route: '/reports', status: 'active', module: 'reports' },
        { labelKey: 'nav.audit', icon: 'audit', route: '/audit', status: 'active', module: 'audit' },
        { labelKey: 'nav.workflows', icon: 'share', route: '/workflows', status: 'active', module: 'workflows' },
        { labelKey: 'nav.notificationRules', icon: 'filter', route: '/admin/notification-rules', status: 'active', module: 'notification-rules' },
        { labelKey: 'nav.administration', icon: 'lock', route: '/administration', status: 'active', module: 'administration' }
      ]
    }
    // Future modules (QMS, CAPA, Deviation, Audit, EBR, MES, LIMS, Workflow, AI …)
    // are intentionally NOT pre-listed to keep the sidebar short (fits without
    // scrolling). Register them here via `register()` / `activate()` when built.
  ]);

  readonly sections = this.store.asReadonly();

  /** Add a nav entry to a section (creating the section if needed) — for future modules. */
  register(sectionTitleKey: string | null, entry: NavEntry): void {
    this.store.update((sections) => {
      const existing = sections.find((s) => s.titleKey === sectionTitleKey);
      if (existing) {
        return sections.map((s) => (s === existing ? { ...s, items: [...s.items, entry] } : s));
      }
      return [...sections, { titleKey: sectionTitleKey, items: [entry] }];
    });
  }

  /** Activate a pre-registered future module (or add a new one) with a real route. */
  activate(module: string, route: string): void {
    this.store.update((sections) =>
      sections.map((s) => ({
        ...s,
        items: s.items.map((i) => (i.module === module ? { ...i, route, status: 'active' as NavStatus } : i))
      }))
    );
  }
}
