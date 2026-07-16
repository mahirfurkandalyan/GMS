import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import {
  AdminService, SystemInfo, AdminCustomer, AdminProject, AdminEnvironment, AdminUser, AdminRole,
  ADMIN_NAV, AdminSection, FUTURE_FEATURES, CONFIG_STATUSES, SYSTEM_ROLES
} from '../../core/admin.service';
import { LanguageService } from '../../core/language.service';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPage } from '../../shared/ui/page/page';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsButton } from '../../shared/ui/button/button';
import { GmsField, GmsInput } from '../../shared/ui/field/field';
import { GmsModal } from '../../shared/ui/dialog/dialog';
import { GmsFormSection } from '../../shared/ui/form-section/form-section';
import { GmsDataGrid, GmsCellDef, ColumnDef, RowActionEvent } from '../../shared/ui/data-grid/data-grid';
import { STANDARD_ROW_ACTIONS } from '../../shared/ui/data-grid/presets';
import { GmsState } from '../../shared/ui/state/state';
import { GmsStat } from '../../shared/ui/stat/stat';
import { GmsSectionNav, SectionNavGroup } from '../../shared/ui/section-nav/section-nav';
import { GmsContextSection } from '../../shared/ui/context-panel/context-section';
import { GmsActivityFeed, ActivityItem } from '../../shared/ui/activity-feed/activity-feed';
import { GmsItemList, LinkItem } from '../../shared/ui/item-list/item-list';
import { ToastService } from '../../shared/ui/toast/toast';
import { ConfirmService } from '../../shared/ui/dialog/dialog';
import { STATUS_BADGES } from '../../shared/ui/badge/badge';

/**
 * ADMIN_NAV / AdminSection text lives in core/admin.service.ts (out of scope here), so
 * group titles and section labels/hints are translated locally by key instead.
 */
const GROUP_LABEL_KEYS: Record<string, string> = {
  'Kuruluş': 'administration.navGroup.organization',
  'Erişim Yönetimi': 'administration.navGroup.access',
  'Sistem': 'administration.navGroup.system'
};
const SECTION_LABEL_KEYS: Record<string, string> = {
  general: 'administration.section.general',
  customers: 'administration.section.customers',
  projects: 'administration.section.projects',
  environments: 'administration.section.environments',
  users: 'administration.section.users',
  roles: 'administration.section.roles',
  teams: 'administration.section.teams',
  templates: 'administration.section.templates',
  notifications: 'administration.section.notifications',
  integrations: 'administration.section.integrations',
  'system-settings': 'administration.section.systemSettings'
};
const SECTION_HINT_KEYS: Record<string, string> = {
  general: 'administration.sectionHint.general',
  customers: 'administration.sectionHint.customers',
  projects: 'administration.sectionHint.projects',
  environments: 'administration.sectionHint.environments',
  users: 'administration.sectionHint.users',
  roles: 'administration.sectionHint.roles',
  teams: 'administration.sectionHint.teams',
  templates: 'administration.sectionHint.templates',
  notifications: 'administration.sectionHint.notifications',
  integrations: 'administration.sectionHint.integrations',
  'system-settings': 'administration.sectionHint.systemSettings'
};
const ENTITY_LABEL_KEYS: Record<string, string> = {
  customer: 'administration.entity.customer',
  project: 'administration.entity.project',
  environment: 'administration.entity.environment',
  user: 'administration.entity.user'
};

@Component({
  selector: 'app-administration',
  imports: [
    FormsModule, DatePipe, GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsField, GmsInput,
    GmsModal, GmsFormSection, GmsDataGrid, GmsCellDef, GmsState, GmsStat, GmsSectionNav,
    GmsContextSection, GmsActivityFeed, GmsItemList, TranslocoPipe
  ],
  providers: [provideTranslocoScope('administration')],
  templateUrl: './administration.html',
  styleUrl: './administration.scss'
})
export class Administration implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  protected readonly nav = ADMIN_NAV;
  protected readonly navGroups = computed<SectionNavGroup[]>(() => {
    this.language.current();
    return ADMIN_NAV.map((g) => ({
      title: g.title ? this.transloco.translate(GROUP_LABEL_KEYS[g.title] ?? g.title) : null,
      items: g.sections.map((s) => ({
        key: s.key,
        label: this.transloco.translate(SECTION_LABEL_KEYS[s.key] ?? s.label),
        icon: s.icon,
        badge: s.implemented ? undefined : this.transloco.translate('administration.comingSoon')
      }))
    }));
  });
  protected readonly statuses = CONFIG_STATUSES;
  protected readonly roles = SYSTEM_ROLES;
  protected readonly statusLabel = (s: string): string => {
    const key = STATUS_BADGES[s]?.labelKey;
    return key ? this.transloco.translate(key) : s;
  };

  protected readonly activeSection = signal<string>('general');
  protected readonly activeMeta = computed<AdminSection>(() => {
    this.language.current();
    let found: AdminSection | undefined;
    for (const g of this.nav) {
      found = g.sections.find((x) => x.key === this.activeSection());
      if (found) break;
    }
    const s = found ?? this.nav[0].sections[0];
    return {
      ...s,
      label: this.transloco.translate(SECTION_LABEL_KEYS[s.key] ?? s.label),
      hint: this.transloco.translate(SECTION_HINT_KEYS[s.key] ?? s.hint)
    };
  });
  protected readonly futureFeatures = computed(() => FUTURE_FEATURES[this.activeSection()] ?? []);
  protected readonly placeholderText = computed(
    () => this.activeMeta().hint + '. ' + this.transloco.translate('administration.placeholderSuffix')
  );

  // Data
  protected readonly system = signal<SystemInfo | null>(null);
  protected readonly customers = signal<AdminCustomer[]>([]);
  protected readonly projects = signal<AdminProject[]>([]);
  protected readonly environments = signal<AdminEnvironment[]>([]);
  protected readonly users = signal<AdminUser[]>([]);
  protected readonly rolesList = signal<AdminRole[]>([]);

  // Filters — customers
  protected readonly fCustSearch = signal('');
  protected readonly fCustStatus = signal('');
  // Filters — projects
  protected readonly fProjSearch = signal('');
  protected readonly fProjCustomer = signal('');
  protected readonly fProjStatus = signal('');
  // Filters — users
  protected readonly fUserSearch = signal('');
  protected readonly fUserRole = signal('');
  protected readonly fUserStatus = signal('');

  protected readonly customerOptions = computed(() => [...new Set(this.projects().map((p) => p.customerName))].sort());

  protected readonly filteredCustomers = computed(() => {
    const q = this.fCustSearch().trim().toLocaleLowerCase('tr');
    return this.customers().filter(
      (c) =>
        (!q || c.name.toLocaleLowerCase('tr').includes(q) || c.code.toLocaleLowerCase('tr').includes(q)) &&
        (!this.fCustStatus() || c.status === this.fCustStatus())
    );
  });
  protected readonly filteredProjects = computed(() => {
    const q = this.fProjSearch().trim().toLocaleLowerCase('tr');
    return this.projects().filter(
      (p) =>
        (!q || p.name.toLocaleLowerCase('tr').includes(q) || p.code.toLocaleLowerCase('tr').includes(q)) &&
        (!this.fProjCustomer() || p.customerName === this.fProjCustomer()) &&
        (!this.fProjStatus() || p.status === this.fProjStatus())
    );
  });
  protected readonly filteredUsers = computed(() => {
    const q = this.fUserSearch().trim().toLocaleLowerCase('tr');
    return this.users().filter(
      (u) =>
        (!q || u.fullName.toLocaleLowerCase('tr').includes(q) || u.email.toLocaleLowerCase('tr').includes(q)) &&
        (!this.fUserRole() || u.role === this.fUserRole()) &&
        (!this.fUserStatus() || u.status === this.fUserStatus())
    );
  });

  // Columns
  protected readonly customerColumns = computed<ColumnDef[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { key: 'code', header: t('administration.col.customerCode'), sticky: true, sortable: true, width: '150px' },
      { key: 'name', header: t('administration.col.customerName'), sortable: true },
      { key: 'status', header: t('administration.col.status'), type: 'badge', badgeKind: 'status', sortable: true },
      { key: 'projectCount', header: t('administration.col.projects'), width: '110px' },
      { key: 'createdAt', header: t('administration.col.createdAt'), sortable: true }
    ];
  });
  protected readonly projectColumns = computed<ColumnDef[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { key: 'code', header: t('administration.col.projectCode'), sticky: true, sortable: true, width: '150px' },
      { key: 'name', header: t('administration.col.projectName'), sortable: true },
      { key: 'customerName', header: t('administration.col.customer') },
      { key: 'owner', header: t('administration.col.owner') },
      { key: 'status', header: t('administration.col.status'), type: 'badge', badgeKind: 'status', sortable: true },
      { key: 'environmentCount', header: t('administration.col.environmentCount'), width: '130px' }
    ];
  });
  protected readonly environmentColumns = computed<ColumnDef[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { key: 'name', header: t('administration.col.environmentName'), sticky: true, sortable: true, width: '160px' },
      { key: 'projectName', header: t('administration.col.project') },
      { key: 'type', header: t('administration.col.type'), type: 'badge', badgeKind: 'environment', sortable: true },
      { key: 'status', header: t('administration.col.status'), type: 'badge', badgeKind: 'status', sortable: true },
      { key: 'lastDeployment', header: t('administration.col.lastDeployment'), sortable: true }
    ];
  });
  protected readonly userColumns = computed<ColumnDef[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { key: 'fullName', header: t('administration.col.fullName'), sticky: true, sortable: true, width: '180px' },
      { key: 'email', header: t('administration.col.email') },
      { key: 'role', header: t('administration.col.role'), sortable: true },
      { key: 'department', header: t('administration.col.department') },
      { key: 'status', header: t('administration.col.status'), type: 'badge', badgeKind: 'status', sortable: true },
      { key: 'lastLogin', header: t('administration.col.lastLogin'), sortable: true }
    ];
  });
  protected readonly roleColumns = computed<ColumnDef[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { key: 'name', header: t('administration.col.roleName'), sticky: true, sortable: true, width: '170px' },
      { key: 'description', header: t('administration.col.description') },
      { key: 'userCount', header: t('administration.col.users'), width: '110px' },
      { key: 'permissions', header: t('administration.col.permissions'), width: '110px' },
      { key: 'status', header: t('administration.col.status'), type: 'badge', badgeKind: 'status', sortable: true }
    ];
  });

  protected readonly rowActions = STANDARD_ROW_ACTIONS;

  // Right panel (reusable)
  protected readonly recentConfigs = computed<LinkItem[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { id: 'cfg1', label: t('administration.recentConfig.prodApproval'), hint: t('administration.recentConfig.prodApprovalHint'), route: '/administration', icon: 'server' },
      { id: 'cfg2', label: t('administration.recentConfig.newRole'), hint: t('administration.recentConfig.newRoleHint'), route: '/administration', icon: 'lock' },
      { id: 'cfg3', label: t('administration.recentConfig.customerAdded'), hint: 'CUST-003', route: '/administration', icon: 'briefcase' }
    ];
  });
  protected readonly systemActivity = computed<ActivityItem[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { actor: 'System Administrator', action: t('administration.activity.backupUpdated'), time: t('administration.time.twoHoursAgo'), icon: 'clock' },
      { actor: 'Furkan Demir', action: t('administration.activity.envAdded'), time: t('administration.time.fiveHoursAgo'), icon: 'server' },
      { actor: 'Zeynep Şahin', action: t('administration.activity.userDeactivated'), time: t('administration.time.yesterday'), icon: 'employees' }
    ];
  });
  protected readonly recentChanges: LinkItem[] = [
    { id: 'ch1', label: 'CHG-2026-019', hint: 'Güvenlik yaması', route: '/changes', icon: 'change' },
    { id: 'ch2', label: 'CHG-2026-017', hint: 'API sürüm yükseltme', route: '/changes', icon: 'change' }
  ];

  // Create-customer modal
  protected readonly custModalOpen = signal(false);
  protected readonly custName = signal('');
  protected readonly custError = signal<string | null>(null);
  protected readonly submitting = signal(false);

  ngOnInit(): void {
    const section = this.route.snapshot.queryParamMap.get('section');
    if (section) this.activeSection.set(section);
    this.admin.getSystemInfo().subscribe((s) => this.system.set(s));
    this.admin.getCustomers().subscribe((c) => this.customers.set(c));
    this.admin.getProjects().subscribe((p) => this.projects.set(p));
    this.admin.getEnvironments().subscribe((e) => this.environments.set(e));
    this.admin.getUsers().subscribe((u) => this.users.set(u));
    this.admin.getRoles().subscribe((r) => this.rolesList.set(r));
  }

  onSectionChange(key: string): void {
    this.activeSection.set(key);
    this.router.navigate([], { queryParams: { section: key }, replaceUrl: true });
  }

  // Generic actions
  onRowAction(event: RowActionEvent, entityKey: string): void {
    const row = event.row as { code?: string; name?: string; fullName?: string };
    const id = row.code ?? row.fullName ?? row.name ?? this.transloco.translate('administration.unknownRecord');
    const entityLabel = this.transloco.translate(ENTITY_LABEL_KEYS[entityKey] ?? entityKey);
    switch (event.action) {
      case 'edit':
        this.toast.info(this.transloco.translate('administration.toast.editSoon', { id }));
        break;
      case 'duplicate':
        this.toast.success(
          this.transloco.translate('administration.toast.duplicated', { id }),
          this.transloco.translate('administration.toast.duplicatedTitle')
        );
        break;
      case 'copy-link':
        navigator.clipboard?.writeText(`${location.origin}/administration?section=${this.activeSection()}`);
        this.toast.success(this.transloco.translate('administration.toast.linkCopied'));
        break;
      case 'archive':
        this.toast.undo(
          this.transloco.translate('administration.toast.archived', { id }),
          () => this.toast.info(this.transloco.translate('administration.toast.undone'))
        );
        break;
      case 'delete':
        this.confirm.ask({
          title: this.transloco.translate('administration.deleteConfirmTitle', { entity: entityLabel }),
          message: this.transloco.translate('administration.deleteConfirmMessage', { id }),
          confirmText: this.transloco.translate('administration.delete'),
          variant: 'danger'
        }).then((ok) => {
          if (ok) {
            this.toast.undo(
              this.transloco.translate('administration.toast.deleted', { id }),
              () => this.toast.info(this.transloco.translate('administration.toast.deleteUndone'))
            );
          }
        });
        break;
      default:
        this.toast.info(this.transloco.translate('administration.toast.actionSoon', { id }));
    }
  }

  onExport(): void {
    this.toast.info(this.transloco.translate('administration.toast.exportSoon'));
  }
  onNewProject(): void {
    this.toast.info(this.transloco.translate('administration.toast.newProjectSoon'));
  }

  // Customer create
  openCustModal(): void {
    this.custName.set(''); this.custError.set(null); this.custModalOpen.set(true);
  }
  onCreateCustomer(): void {
    const name = this.custName().trim();
    if (!name) { this.custError.set(this.transloco.translate('administration.createModal.errorRequired')); return; }
    this.submitting.set(true);
    this.admin.createCustomer(name).subscribe({
      next: (c) => {
        this.submitting.set(false);
        this.custModalOpen.set(false);
        this.admin.getCustomers().subscribe((list) => this.customers.set(list));
        this.toast.success(
          this.transloco.translate('administration.toast.customerCreated', { code: c.code, name: c.name }),
          this.transloco.translate('administration.toast.customerCreatedTitle')
        );
      },
      error: () => { this.submitting.set(false); this.custError.set(this.transloco.translate('administration.createModal.errorCreateFailed')); }
    });
  }

  resetCustomerFilters(): void { this.fCustSearch.set(''); this.fCustStatus.set(''); }
  resetProjectFilters(): void { this.fProjSearch.set(''); this.fProjCustomer.set(''); this.fProjStatus.set(''); }
  resetUserFilters(): void { this.fUserSearch.set(''); this.fUserRole.set(''); this.fUserStatus.set(''); }

  // General helpers
  protected readonly healthLabel = (h: string): string => {
    const key = h === 'healthy' ? 'administration.health.healthy' : h === 'degraded' ? 'administration.health.degraded' : 'administration.health.down';
    return this.transloco.translate(key);
  };
  protected readonly healthTone = (h: string) => (h === 'healthy' ? 'success' : h === 'degraded' ? 'warning' : 'danger');
}
