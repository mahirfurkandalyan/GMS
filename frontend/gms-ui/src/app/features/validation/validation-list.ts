import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { ValidationService, Validation, VALIDATION_RESULTS, SEVERITIES } from '../../core/validation.service';
import { LanguageService } from '../../core/language.service';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPage } from '../../shared/ui/page/page';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsButton } from '../../shared/ui/button/button';
import { GmsDataGrid, GmsCellDef, ColumnDef, RowActionEvent } from '../../shared/ui/data-grid/data-grid';
import { MenuItem } from '../../shared/ui/menu/menu';
import { GmsState } from '../../shared/ui/state/state';
import { GmsContextSection } from '../../shared/ui/context-panel/context-section';
import { GmsRelationshipStrip, RelationNode } from '../../shared/ui/relationship-strip/relationship-strip';
import { GmsActivityFeed, ActivityItem } from '../../shared/ui/activity-feed/activity-feed';
import { GmsItemList, LinkItem } from '../../shared/ui/item-list/item-list';
import { ToastService } from '../../shared/ui/toast/toast';
import { STATUS_BADGES, PRIORITY_BADGES } from '../../shared/ui/badge/badge';

@Component({
  selector: 'app-validation-list',
  imports: [
    FormsModule,
    DatePipe,
    GmsIcon,
    GmsPage,
    GmsPageHeader,
    GmsButton,
    GmsDataGrid,
    GmsCellDef,
    GmsState,
    GmsContextSection,
    GmsRelationshipStrip,
    GmsActivityFeed,
    GmsItemList,
    TranslocoPipe
  ],
  providers: [provideTranslocoScope('validation')],
  templateUrl: './validation-list.html',
  styleUrl: './validation-list.scss'
})
export class ValidationList implements OnInit {
  private readonly validationService = inject(ValidationService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  protected readonly resultOptions = VALIDATION_RESULTS;
  protected readonly severityOptions = SEVERITIES;
  protected readonly resultLabelKey = (r: string) => STATUS_BADGES[r]?.labelKey ?? r;
  protected readonly severityLabelKey = (s: string) => PRIORITY_BADGES[s]?.labelKey ?? s;

  protected readonly rows = signal<Validation[]>([]);
  protected readonly loading = signal(false);

  protected readonly fSearch = signal('');
  protected readonly fRelease = signal('');
  protected readonly fProject = signal('');
  protected readonly fEnv = signal('');
  protected readonly fResult = signal('');
  protected readonly fSeverity = signal('');
  protected readonly fOwner = signal('');
  protected readonly fDate = signal('');

  protected readonly releaseOptions = computed(() => [...new Set(this.rows().map((r) => r.releaseCode))].sort());
  protected readonly projectOptions = computed(() => [...new Set(this.rows().map((r) => r.projectName))].sort());
  protected readonly envOptions = computed(() => [...new Set(this.rows().map((r) => r.environmentName))].sort());
  protected readonly ownerOptions = computed(() => [...new Set(this.rows().map((r) => r.executedBy))].sort());

  protected readonly filteredRows = computed(() => {
    const q = this.fSearch().trim().toLocaleLowerCase('tr');
    return this.rows().filter((v) => {
      const matchesText =
        !q ||
        v.code.toLocaleLowerCase('tr').includes(q) ||
        v.changeCode.toLocaleLowerCase('tr').includes(q) ||
        v.releaseCode.toLocaleLowerCase('tr').includes(q) ||
        v.rule.toLocaleLowerCase('tr').includes(q);
      return (
        matchesText &&
        (!this.fRelease() || v.releaseCode === this.fRelease()) &&
        (!this.fProject() || v.projectName === this.fProject()) &&
        (!this.fEnv() || v.environmentName === this.fEnv()) &&
        (!this.fResult() || v.result === this.fResult()) &&
        (!this.fSeverity() || v.severity === this.fSeverity()) &&
        (!this.fOwner() || v.executedBy === this.fOwner()) &&
        (!this.fDate() || v.executedAt.slice(0, 10) >= this.fDate())
      );
    });
  });

  protected readonly hasFilters = computed(
    () => !!(this.fSearch() || this.fRelease() || this.fProject() || this.fEnv() || this.fResult() || this.fSeverity() || this.fOwner() || this.fDate())
  );

  protected readonly failedCount = computed(() => this.rows().filter((v) => v.result === 'Failed').length);

  protected readonly columns = computed<ColumnDef[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { key: 'code', header: t('validation.columns.code'), sticky: true, sortable: true, width: '150px' },
      { key: 'changeCode', header: t('validation.columns.change') },
      { key: 'releaseCode', header: t('validation.columns.release') },
      { key: 'projectName', header: t('validation.columns.project'), sortable: true },
      { key: 'environmentName', header: t('validation.columns.environment'), type: 'badge', badgeKind: 'environment' },
      { key: 'rule', header: t('validation.columns.rule') },
      { key: 'severity', header: t('validation.columns.severity'), type: 'badge', badgeKind: 'priority' },
      { key: 'result', header: t('validation.columns.result'), type: 'badge', badgeKind: 'status', sortable: true },
      { key: 'executedBy', header: t('validation.columns.executedBy') },
      { key: 'executedAt', header: t('validation.columns.executedAt'), sortable: true }
    ];
  });

  protected readonly rowActions = computed<MenuItem[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { label: t('validation.rowActions.open'), value: 'open', icon: 'search' },
      { label: t('validation.rowActions.change'), value: 'change', icon: 'change' },
      { label: t('validation.rowActions.copyLink'), value: 'copy-link', icon: 'share' }
    ];
  });

  protected readonly relationChain = computed<RelationNode[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { type: t('validation.relationType.area'), label: t('validation.domainName'), icon: 'shield', route: '/hub' },
      { type: t('validation.relationType.module'), label: t('validation.moduleName'), icon: 'shield', current: true }
    ];
  });
  protected readonly panelActivity = computed<ActivityItem[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { actor: 'System Administrator', action: `VAL-2026-001 ${t('validation.activity.ran')}`, time: '2 saat önce', icon: 'shield' },
      { actor: 'Ayşe Yılmaz', action: `VAL-2026-002 ${t('validation.activity.foundFailed')}`, time: '4 saat önce', icon: 'close' },
      { actor: 'Ali Vural', action: `VAL-2026-003 ${t('validation.activity.passed')}`, time: 'Dün', icon: 'check' }
    ];
  });
  protected readonly relatedChanges: LinkItem[] = [
    { id: 'c1', label: 'CHG-2026-014', hint: 'Veritabanı şeması güncellemesi', route: '/changes/chg-2026-014', icon: 'change' }
  ];

  ngOnInit(): void {
    this.load();
  }

  openValidation(row: Validation): void {
    this.router.navigate(['/validation', row.id]);
  }

  resetFilters(): void {
    this.fSearch.set(''); this.fRelease.set(''); this.fProject.set(''); this.fEnv.set('');
    this.fResult.set(''); this.fSeverity.set(''); this.fOwner.set(''); this.fDate.set('');
  }

  onRowAction(event: RowActionEvent): void {
    const v = event.row as Validation;
    if (event.action === 'open') {
      this.openValidation(v);
    } else if (event.action === 'change') {
      this.router.navigate(['/changes', v.changeId]);
    } else if (event.action === 'copy-link') {
      navigator.clipboard?.writeText(`${location.origin}/validation/${v.id}`);
      this.toast.success(this.transloco.translate('validation.toast.linkCopied'));
    }
  }

  onExport(): void {
    this.toast.info(this.transloco.translate('validation.toast.exportSoon'));
  }

  reload(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.validationService.getValidations().subscribe((v) => {
      this.rows.set(v);
      this.loading.set(false);
    });
  }
}
