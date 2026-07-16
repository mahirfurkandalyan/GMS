import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { AuditService, AuditRecord, AUDIT_ACTIONS, AUDIT_MODULES, AUDIT_OBJECT_TYPES } from '../../core/audit.service';
import { LanguageService } from '../../core/language.service';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPage } from '../../shared/ui/page/page';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsButton } from '../../shared/ui/button/button';
import { GmsMenu, MenuItem } from '../../shared/ui/menu/menu';
import { GmsFilterBar } from '../../shared/ui/filter-bar/filter-bar';
import { GmsSectionNav, SectionNavGroup } from '../../shared/ui/section-nav/section-nav';
import { GmsWidget } from '../../shared/ui/widget/widget';
import { GmsDataGrid, GmsCellDef, ColumnDef } from '../../shared/ui/data-grid/data-grid';
import { GmsState } from '../../shared/ui/state/state';
import { GmsContextSection } from '../../shared/ui/context-panel/context-section';
import { GmsAuditList } from '../../shared/ui/audit-list/audit-list';
import { GmsItemList } from '../../shared/ui/item-list/item-list';
import { ToastService } from '../../shared/ui/toast/toast';
import { recentActivities, criticalEvents, recentSecurity, mostActiveUsers, mostModifiedObjects, localActionMeta, localResultMeta, moduleLabel } from './audit-vm';

@Component({
  selector: 'app-audit-list',
  imports: [
    FormsModule, DatePipe, GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsMenu, GmsFilterBar,
    GmsSectionNav, GmsWidget, GmsDataGrid, GmsCellDef, GmsState, GmsContextSection, GmsAuditList, GmsItemList, TranslocoPipe
  ],
  providers: [provideTranslocoScope('audit')],
  templateUrl: './audit-list.html',
  styleUrl: './audit-list.scss'
})
export class AuditList implements OnInit {
  private readonly auditService = inject(AuditService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  protected readonly actionMeta = (a: string) => localActionMeta(a, this.transloco);
  protected readonly resultMeta = (r: string) => localResultMeta(r, this.transloco);
  protected readonly moduleLabel = (m: string) => moduleLabel(m, this.transloco);
  protected readonly actions = AUDIT_ACTIONS;
  protected readonly modules = AUDIT_MODULES;
  protected readonly objectTypes = AUDIT_OBJECT_TYPES;

  protected readonly records = signal<AuditRecord[]>([]);
  protected readonly loading = signal(true);

  // Section registry → reusable section nav
  private readonly sections = computed(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { key: 'overview', label: t.translate('audit.section.overview'), icon: 'dashboard', implemented: true, hint: t.translate('audit.section.overviewHint') },
      { key: 'records', label: t.translate('audit.section.records'), icon: 'audit', implemented: true, hint: t.translate('audit.section.recordsHint') },
      { key: 'users', label: t.translate('audit.section.users'), icon: 'employees', implemented: true, hint: t.translate('audit.section.usersHint') },
      { key: 'security', label: t.translate('audit.section.security'), icon: 'shield', implemented: true, hint: t.translate('audit.section.securityHint') },
      { key: 'compliance', label: t.translate('audit.section.compliance'), icon: 'approval', implemented: true, hint: t.translate('audit.section.complianceHint') },
      { key: 'retention', label: t.translate('audit.section.retention'), icon: 'clock', implemented: false, hint: t.translate('audit.section.retentionHint') }
    ];
  });
  protected readonly navGroups = computed<SectionNavGroup[]>(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { title: null, items: [this.toNav('overview')] },
      { title: t.translate('audit.navGroup.review'), items: ['records', 'users', 'security', 'compliance'].map((k) => this.toNav(k)) },
      { title: t.translate('audit.navGroup.governance'), items: [this.toNav('retention')] }
    ];
  });
  private toNav(key: string) {
    const s = this.sections().find((x) => x.key === key)!;
    return { key: s.key, label: s.label, icon: s.icon as never, badge: s.implemented ? undefined : this.transloco.translate('audit.soon') };
  }
  protected readonly activeSection = signal('overview');
  protected readonly sectionMeta = computed(() => this.sections().find((s) => s.key === this.activeSection()) ?? this.sections()[0]);

  // Filters
  protected readonly fSearch = signal('');
  protected readonly fDate = signal('');
  protected readonly fUser = signal('');
  protected readonly fModule = signal('');
  protected readonly fObjectType = signal('');
  protected readonly fAction = signal('');
  protected readonly fEnv = signal('');

  protected readonly userOptions = computed(() => [...new Set(this.records().map((r) => r.user))].sort());
  protected readonly envOptions = computed(() => [...new Set(this.records().map((r) => r.environment))].sort());

  private readonly filtered = computed(() => {
    const q = this.fSearch().trim().toLocaleLowerCase('tr');
    return this.records().filter((r) => {
      const matchesText = !q
        || r.code.toLocaleLowerCase('tr').includes(q)
        || r.user.toLocaleLowerCase('tr').includes(q)
        || r.objectName.toLocaleLowerCase('tr').includes(q)
        || r.objectId.toLocaleLowerCase('tr').includes(q);
      return matchesText
        && (!this.fUser() || r.user === this.fUser())
        && (!this.fModule() || r.module === this.fModule())
        && (!this.fObjectType() || r.objectType === this.fObjectType())
        && (!this.fAction() || r.action === this.fAction())
        && (!this.fEnv() || r.environment === this.fEnv())
        && (!this.fDate() || r.timestamp.slice(0, 10) >= this.fDate());
    });
  });

  /** Records shown in the active section's grid. */
  protected readonly sectionRecords = computed(() => {
    const list = this.filtered();
    switch (this.activeSection()) {
      case 'users': return list.filter((r) => r.category === 'user');
      case 'security': return list.filter((r) => r.category === 'security');
      case 'compliance': return list.filter((r) => r.category === 'compliance');
      default: return list;
    }
  });

  // Overview aggregates
  protected readonly recent = computed(() => { this.language.current(); return recentActivities(this.records(), this.transloco); });
  protected readonly critical = computed(() => { this.language.current(); return criticalEvents(this.records(), this.transloco); });
  protected readonly security = computed(() => { this.language.current(); return recentSecurity(this.records(), this.transloco); });
  protected readonly activeUsers = computed(() => { this.language.current(); return mostActiveUsers(this.records(), this.transloco); });
  protected readonly modifiedObjects = computed(() => { this.language.current(); return mostModifiedObjects(this.records(), this.transloco); });

  protected readonly columns = computed<ColumnDef[]>(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { key: 'timestamp', header: t.translate('audit.column.timestamp'), sticky: true, sortable: true, width: '150px' },
      { key: 'user', header: t.translate('audit.column.user'), sortable: true },
      { key: 'module', header: t.translate('audit.column.module'), sortable: true },
      { key: 'objectName', header: t.translate('audit.column.objectName') },
      { key: 'objectId', header: t.translate('audit.column.objectId') },
      { key: 'action', header: t.translate('audit.column.action'), sortable: true },
      { key: 'previousPreview', header: t.translate('audit.column.previousValue') },
      { key: 'currentPreview', header: t.translate('audit.column.newValue') },
      { key: 'environment', header: t.translate('audit.column.environment'), type: 'badge', badgeKind: 'environment' },
      { key: 'result', header: t.translate('audit.column.result') }
    ];
  });

  protected readonly exportItems = computed<MenuItem[]>(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { label: t.translate('audit.export.pdf'), value: 'pdf', icon: 'document' },
      { label: t.translate('audit.export.excel'), value: 'excel', icon: 'grid' },
      { label: t.translate('audit.export.csv'), value: 'csv', icon: 'document' }
    ];
  });
  protected readonly siemItems = computed<MenuItem[]>(() => {
    this.language.current();
    const t = this.transloco;
    const soon = t.translate('audit.soon');
    return [
      { label: `Microsoft Sentinel (${soon})`, value: 'sentinel', icon: 'shield', disabled: true },
      { label: `Splunk (${soon})`, value: 'splunk', icon: 'search', disabled: true },
      { label: `Elastic (${soon})`, value: 'elastic', icon: 'search', disabled: true },
      { label: `Azure Monitor (${soon})`, value: 'azure', icon: 'server', disabled: true },
      { label: `QRadar (${soon})`, value: 'qradar', icon: 'audit', disabled: true }
    ];
  });

  ngOnInit(): void {
    const s = this.route.snapshot.queryParamMap.get('section');
    if (s) this.activeSection.set(s);
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.auditService.getRecords().subscribe((r) => {
      this.records.set(r);
      this.loading.set(false);
    });
  }

  onSectionChange(key: string): void {
    this.activeSection.set(key);
    this.router.navigate([], { queryParams: { section: key }, replaceUrl: true });
  }

  openRecord(row: AuditRecord): void {
    this.router.navigate(['/audit', row.id]);
  }

  onExport(kind: string): void {
    this.toast.info(this.transloco.translate('audit.toast.exportSoon', { kind: kind.toUpperCase() }));
  }
  onSiem(): void {
    this.toast.info(this.transloco.translate('audit.toast.siemSoon'));
  }
  onRefresh(): void {
    this.load();
    this.toast.success(this.transloco.translate('audit.toast.refreshed'));
  }
  onResetFilters(): void {
    this.fDate.set(''); this.fUser.set(''); this.fModule.set(''); this.fObjectType.set(''); this.fAction.set(''); this.fEnv.set('');
  }
}
