import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { ApprovalService, Approval, APPROVAL_STATUSES, APPROVAL_TYPES } from '../../core/approval.service';
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
import { STATUS_BADGES } from '../../shared/ui/badge/badge';
import { relativeTime, dateLocale } from './approval-vm';

@Component({
  selector: 'app-approval-list',
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
  providers: [provideTranslocoScope('approvals')],
  templateUrl: './approval-list.html',
  styleUrl: './approval-list.scss'
})
export class ApprovalList implements OnInit {
  private readonly approvalService = inject(ApprovalService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  protected readonly statusOptions = APPROVAL_STATUSES;
  protected readonly typeOptions = APPROVAL_TYPES;
  protected readonly statusLabelKey = (s: string) => STATUS_BADGES[s]?.labelKey ?? s;
  protected readonly rel = (iso: string) =>
    relativeTime(iso, (k, p) => this.transloco.translate(k, p), dateLocale(this.language.current()));

  protected readonly rows = signal<Approval[]>([]);
  protected readonly loading = signal(false);

  // Filters
  protected readonly fSearch = signal('');
  protected readonly fStatus = signal('');
  protected readonly fRelease = signal('');
  protected readonly fProject = signal('');
  protected readonly fEnv = signal('');
  protected readonly fType = signal('');
  protected readonly fOwner = signal('');
  protected readonly fDate = signal('');

  protected readonly releaseOptions = computed(() => [...new Set(this.rows().map((r) => r.releaseCode))].sort());
  protected readonly projectOptions = computed(() => [...new Set(this.rows().map((r) => r.projectName))].sort());
  protected readonly envOptions = computed(() => [...new Set(this.rows().map((r) => r.environmentName))].sort());
  protected readonly ownerOptions = computed(() => [...new Set(this.rows().map((r) => r.currentApprover).filter((o) => o !== '—'))].sort());

  protected readonly filteredRows = computed(() => {
    const q = this.fSearch().trim().toLocaleLowerCase('tr');
    return this.rows().filter((a) => {
      const matchesText =
        !q ||
        a.code.toLocaleLowerCase('tr').includes(q) ||
        a.title.toLocaleLowerCase('tr').includes(q) ||
        a.releaseCode.toLocaleLowerCase('tr').includes(q) ||
        a.currentApprover.toLocaleLowerCase('tr').includes(q);
      return (
        matchesText &&
        (!this.fStatus() || a.status === this.fStatus()) &&
        (!this.fRelease() || a.releaseCode === this.fRelease()) &&
        (!this.fProject() || a.projectName === this.fProject()) &&
        (!this.fEnv() || a.environmentName === this.fEnv()) &&
        (!this.fType() || a.approvalType === this.fType()) &&
        (!this.fOwner() || a.currentApprover === this.fOwner()) &&
        (!this.fDate() || a.requestedAt.slice(0, 10) >= this.fDate())
      );
    });
  });

  protected readonly hasFilters = computed(
    () => !!(this.fSearch() || this.fStatus() || this.fRelease() || this.fProject() || this.fEnv() || this.fType() || this.fOwner() || this.fDate())
  );

  protected readonly pendingCount = computed(() => this.rows().filter((a) => a.status === 'Pending').length);

  protected readonly columns = computed<ColumnDef[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { key: 'code', header: t('approvals.columns.code'), sticky: true, sortable: true, width: '150px' },
      { key: 'title', header: t('approvals.columns.title'), sortable: true },
      { key: 'releaseCode', header: t('approvals.columns.release') },
      { key: 'changeCode', header: t('approvals.columns.change') },
      { key: 'stage', header: t('approvals.columns.stage') },
      { key: 'currentApprover', header: t('approvals.columns.approver') },
      { key: 'priority', header: t('approvals.columns.priority'), type: 'badge', badgeKind: 'priority' },
      { key: 'risk', header: t('approvals.columns.risk'), type: 'badge', badgeKind: 'risk' },
      { key: 'status', header: t('approvals.columns.status'), type: 'badge', badgeKind: 'status', sortable: true },
      { key: 'requestedAt', header: t('approvals.columns.requestedAt'), sortable: true },
      { key: 'dueDate', header: t('approvals.columns.dueDate') }
    ];
  });

  protected readonly rowActions = computed<MenuItem[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { label: t('approvals.rowActions.open'), value: 'open', icon: 'search' },
      { label: t('approvals.rowActions.copyLink'), value: 'copy-link', icon: 'share' }
    ];
  });

  // Context panel
  protected readonly relationChain = computed<RelationNode[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { type: t('approvals.relationType.area'), label: t('approvals.domainName'), icon: 'shield', route: '/hub' },
      { type: t('approvals.relationType.module'), label: t('approvals.moduleName'), icon: 'approval', current: true }
    ];
  });
  protected readonly panelActivity = computed<ActivityItem[]>(() => {
    this.language.current();
    const t = (k: string, p?: Record<string, unknown>) => this.transloco.translate(k, p);
    return [
      { actor: 'Architect User', action: t('approvals.activity.listApproved', { code: 'APR-2026-003' }), time: '30 dk önce', icon: 'approval' },
      { actor: 'QA Specialist', action: t('approvals.activity.listViewedRequest'), time: '2 saat önce', icon: 'search' },
      { actor: 'System Administrator', action: t('approvals.activity.listRejected', { code: 'APR-2026-004' }), time: 'Dün', icon: 'close' }
    ];
  });
  protected readonly relatedReleases: LinkItem[] = [
    { id: 'r1', label: 'REL-2026-001', hint: 'EBR Migration · PROD', route: '/releases/07777777-7777-7777-7777-777777777701', icon: 'release' }
  ];

  ngOnInit(): void {
    this.load();
  }

  openApproval(row: Approval): void {
    this.router.navigate(['/approvals', row.id]);
  }

  resetFilters(): void {
    this.fSearch.set(''); this.fStatus.set(''); this.fRelease.set(''); this.fProject.set('');
    this.fEnv.set(''); this.fType.set(''); this.fOwner.set(''); this.fDate.set('');
  }

  onRowAction(event: RowActionEvent): void {
    const a = event.row as Approval;
    if (event.action === 'open') {
      this.openApproval(a);
    } else if (event.action === 'copy-link') {
      navigator.clipboard?.writeText(`${location.origin}/approvals/${a.id}`);
      this.toast.success(this.transloco.translate('approvals.toast.linkCopied'));
    }
  }

  onExport(): void {
    this.toast.info(this.transloco.translate('approvals.toast.exportSoon'));
  }

  reload(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.approvalService.getApprovals().subscribe((approvals) => {
      this.rows.set(approvals);
      this.loading.set(false);
    });
  }
}
