import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { ReportService, ReportSnapshot, TopIssue, Kpi } from '../../core/report.service';
import { LanguageService } from '../../core/language.service';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPage } from '../../shared/ui/page/page';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsButton } from '../../shared/ui/button/button';
import { GmsMenu, MenuItem } from '../../shared/ui/menu/menu';
import { GmsFilterBar } from '../../shared/ui/filter-bar/filter-bar';
import { GmsSectionNav, SectionNavGroup } from '../../shared/ui/section-nav/section-nav';
import { GmsWidget } from '../../shared/ui/widget/widget';
import { GmsChart, ChartDatum } from '../../shared/ui/chart/chart';
import { GmsStat } from '../../shared/ui/stat/stat';
import { GmsDataGrid, GmsCellDef, ColumnDef } from '../../shared/ui/data-grid/data-grid';
import { GmsState } from '../../shared/ui/state/state';
import { GmsContextSection } from '../../shared/ui/context-panel/context-section';
import { GmsActivityFeed } from '../../shared/ui/activity-feed/activity-feed';
import { GmsItemList } from '../../shared/ui/item-list/item-list';
import { ToastService } from '../../shared/ui/toast/toast';
import { STATUS_BADGES, RISK_BADGES } from '../../shared/ui/badge/badge';

/** Report category registry — drives the reusable section nav + placeholders. */
interface ReportCategory {
  key: string;
  labelKey: string;
  icon: string;
  implemented: boolean;
  hintKey: string;
}

/** KPI labels come from core/report.service.ts (out of scope) — mapped locally by kpi.key. */
const KPI_LABEL_KEYS: Record<string, string> = {
  'releases': 'reports.kpi.releases',
  'open-changes': 'reports.kpi.openChanges',
  'pending-approvals': 'reports.kpi.pendingApprovals',
  'failed-exec': 'reports.kpi.failedExec',
  'critical-risks': 'reports.kpi.criticalRisks',
  'avg-approval': 'reports.kpi.avgApproval'
};

@Component({
  selector: 'app-reports',
  imports: [
    FormsModule, DatePipe, GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsMenu, GmsFilterBar,
    GmsSectionNav, GmsWidget, GmsChart, GmsStat, GmsDataGrid, GmsCellDef, GmsState,
    GmsContextSection, GmsActivityFeed, GmsItemList, TranslocoPipe
  ],
  providers: [provideTranslocoScope('reports')],
  templateUrl: './reports.html',
  styleUrl: './reports.scss'
})
export class Reports implements OnInit {
  private readonly reportService = inject(ReportService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  protected readonly loading = signal(true);
  protected readonly snap = signal<ReportSnapshot | null>(null);

  // Category registry
  private readonly categories: ReportCategory[] = [
    { key: 'overview', labelKey: 'reports.category.overview', icon: 'dashboard', implemented: true, hintKey: 'reports.categoryHint.overview' },
    { key: 'releases', labelKey: 'reports.category.releases', icon: 'release', implemented: true, hintKey: 'reports.categoryHint.releases' },
    { key: 'changes', labelKey: 'reports.category.changes', icon: 'change', implemented: true, hintKey: 'reports.categoryHint.changes' },
    { key: 'approvals', labelKey: 'reports.category.approvals', icon: 'approval', implemented: false, hintKey: 'reports.categoryHint.approvals' },
    { key: 'validation', labelKey: 'reports.category.validation', icon: 'shield', implemented: false, hintKey: 'reports.categoryHint.validation' },
    { key: 'execution', labelKey: 'reports.category.execution', icon: 'execution', implemented: false, hintKey: 'reports.categoryHint.execution' },
    { key: 'assets', labelKey: 'reports.category.assets', icon: 'server', implemented: false, hintKey: 'reports.categoryHint.assets' },
    { key: 'documents', labelKey: 'reports.category.documents', icon: 'document', implemented: false, hintKey: 'reports.categoryHint.documents' },
    { key: 'audit', labelKey: 'reports.category.audit', icon: 'audit', implemented: false, hintKey: 'reports.categoryHint.audit' },
    { key: 'custom', labelKey: 'reports.category.custom', icon: 'grid', implemented: false, hintKey: 'reports.categoryHint.custom' }
  ];
  protected readonly navGroups = computed<SectionNavGroup[]>(() => {
    this.language.current();
    const toNav = (key: string) => {
      const c = this.categories.find((x) => x.key === key)!;
      return { key: c.key, label: this.transloco.translate(c.labelKey), icon: c.icon as never, badge: c.implemented ? undefined : this.transloco.translate('reports.comingSoon') };
    };
    return [
      { title: null, items: [toNav('overview')] },
      { title: this.transloco.translate('reports.navGroup.processes'), items: ['releases', 'changes', 'approvals', 'validation', 'execution'].map((k) => toNav(k)) },
      { title: this.transloco.translate('reports.navGroup.assets'), items: ['assets', 'documents'].map((k) => toNav(k)) },
      { title: this.transloco.translate('reports.navGroup.system'), items: ['audit', 'custom'].map((k) => toNav(k)) }
    ];
  });

  protected readonly activeCat = signal('overview');
  protected readonly catMeta = computed(() => {
    this.language.current();
    const c = this.categories.find((c) => c.key === this.activeCat()) ?? this.categories[0];
    return { ...c, label: this.transloco.translate(c.labelKey), hint: this.transloco.translate(c.hintKey) };
  });
  protected readonly categoryPlaceholderText = computed(
    () => this.catMeta().hint + '. ' + this.transloco.translate('reports.categoryPlaceholderSuffix')
  );

  // Reusable filters
  protected readonly fSearch = signal('');
  protected readonly fDate = signal('');
  protected readonly fProject = signal('');
  protected readonly fEnv = signal('');
  protected readonly fRelease = signal('');
  protected readonly fOwner = signal('');
  protected readonly fRisk = signal('');
  protected readonly fStatus = signal('');

  protected readonly exportItems = computed<MenuItem[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { label: t('reports.export.pdf'), value: 'pdf', icon: 'document' },
      { label: t('reports.export.excel'), value: 'excel', icon: 'grid' },
      { label: t('reports.export.csv'), value: 'csv', icon: 'document' },
      { label: t('reports.export.print'), value: 'print', icon: 'document' }
    ];
  });
  protected readonly biItems = computed<MenuItem[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { label: t('reports.bi.powerbi'), value: 'powerbi', icon: 'grid', disabled: true },
      { label: t('reports.bi.grafana'), value: 'grafana', icon: 'activity', disabled: true },
      { label: t('reports.bi.tableau'), value: 'tableau', icon: 'dashboard', disabled: true },
      { label: t('reports.bi.elastic'), value: 'elastic', icon: 'search', disabled: true },
      { label: t('reports.bi.azure'), value: 'azure', icon: 'server', disabled: true }
    ];
  });

  // Table columns (reused across Top Issues & Open Changes)
  protected readonly issueColumns = computed<ColumnDef[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { key: 'object', header: t('reports.col.object'), sticky: true, sortable: true, width: '150px' },
      { key: 'type', header: t('reports.col.type') },
      { key: 'priority', header: t('reports.col.priority'), type: 'badge', badgeKind: 'priority', sortable: true },
      { key: 'status', header: t('reports.col.status'), type: 'badge', badgeKind: 'status', sortable: true },
      { key: 'owner', header: t('reports.col.owner') },
      { key: 'updatedAt', header: t('reports.col.updatedAt'), sortable: true }
    ];
  });

  private matchesFilters(i: TopIssue): boolean {
    const q = this.fSearch().trim().toLocaleLowerCase('tr');
    return (
      (!q || i.object.toLocaleLowerCase('tr').includes(q) || i.owner.toLocaleLowerCase('tr').includes(q)) &&
      (!this.fOwner() || i.owner === this.fOwner()) &&
      (!this.fRisk() || i.priority === this.fRisk()) &&
      (!this.fStatus() || i.status === this.fStatus())
    );
  }
  protected readonly topIssues = computed(() => (this.snap()?.topIssues ?? []).filter((i) => this.matchesFilters(i)));
  protected readonly openChanges = computed(() => (this.snap()?.openChanges ?? []).filter((i) => this.matchesFilters(i)));

  protected readonly riskOptions = ['High', 'Medium', 'Low'];
  protected readonly statusOptions = computed(() => [...new Set((this.snap()?.changeByStatus ?? []).map((d) => d.label))]);

  // KPIs and chart datasets: source data comes from a core service whose category
  // labels are raw status/risk strings — resolved here via the shared badge registry
  // (badge.status.* / badge.risk.*) without touching the core file.
  protected readonly kpis = computed<Kpi[]>(() => {
    this.language.current();
    return (this.snap()?.kpis ?? []).map((k) => ({
      ...k,
      label: KPI_LABEL_KEYS[k.key] ? this.transloco.translate(KPI_LABEL_KEYS[k.key]) : k.label
    }));
  });
  private mapChartLabels(data: ChartDatum[] | undefined, kind: 'status' | 'risk'): ChartDatum[] {
    const registry = kind === 'status' ? STATUS_BADGES : RISK_BADGES;
    return (data ?? []).map((d) => {
      const key = registry[d.label]?.labelKey;
      return key ? { ...d, label: this.transloco.translate(key) } : d;
    });
  }
  protected readonly riskDistribution = computed(() => { this.language.current(); return this.mapChartLabels(this.snap()?.riskDistribution, 'risk'); });
  protected readonly approvalStatus = computed(() => { this.language.current(); return this.mapChartLabels(this.snap()?.approvalStatus, 'status'); });
  protected readonly executionSuccess = computed(() => { this.language.current(); return this.mapChartLabels(this.snap()?.executionSuccess, 'status'); });
  protected readonly releaseStatus = computed(() => { this.language.current(); return this.mapChartLabels(this.snap()?.releaseStatus, 'status'); });
  protected readonly changeByRisk = computed(() => { this.language.current(); return this.mapChartLabels(this.snap()?.changeByRisk, 'risk'); });
  protected readonly changeByStatus = computed(() => { this.language.current(); return this.mapChartLabels(this.snap()?.changeByStatus, 'status'); });

  protected readonly pendingSummary = computed(() => {
    this.language.current();
    const approvals = this.snap()?.kpis?.[2]?.value ?? 0;
    const changes = this.snap()?.kpis?.[1]?.value ?? 0;
    return this.transloco.translate('reports.pendingSummary', { approvals, changes });
  });

  ngOnInit(): void {
    const cat = this.route.snapshot.queryParamMap.get('category');
    if (cat) this.activeCat.set(cat);
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.reportService.getSnapshot().subscribe((s) => {
      this.snap.set(s);
      this.loading.set(false);
    });
  }

  onCategoryChange(key: string): void {
    this.activeCat.set(key);
    this.router.navigate([], { queryParams: { category: key }, replaceUrl: true });
  }

  onExport(kind: string): void {
    this.toast.info(this.transloco.translate('reports.toast.exportSoon', { kind: kind.toUpperCase() }));
  }
  onBi(): void {
    this.toast.info(this.transloco.translate('reports.toast.biSoon'));
  }
  onRefresh(): void {
    this.load();
    this.toast.success(this.transloco.translate('reports.toast.refreshed'));
  }
  onResetFilters(): void {
    this.fDate.set(''); this.fProject.set(''); this.fEnv.set(''); this.fRelease.set('');
    this.fOwner.set(''); this.fRisk.set(''); this.fStatus.set('');
  }
  openIssue(row: TopIssue): void {
    this.router.navigateByUrl(row.route);
  }
}
