import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { ApprovalService, Approval, ApprovalStep, StepStatus } from '../../core/approval.service';
import { RecentService } from '../../core/recent.service';
import { WorkspaceContextService } from '../../core/workspace-context.service';
import { LanguageService } from '../../core/language.service';
import { GmsIcon, IconName } from '../../shared/icon/icon';
import { GmsPage } from '../../shared/ui/page/page';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsButton } from '../../shared/ui/button/button';
import { GmsMenu } from '../../shared/ui/menu/menu';
import { GmsTabs, TabItem } from '../../shared/ui/tabs/tabs';
import { GmsTimeline } from '../../shared/ui/timeline/timeline';
import { GmsActivityFeed } from '../../shared/ui/activity-feed/activity-feed';
import { GmsItemList } from '../../shared/ui/item-list/item-list';
import { GmsRelationshipStrip, RelationNode } from '../../shared/ui/relationship-strip/relationship-strip';
import { GmsContextSection } from '../../shared/ui/context-panel/context-section';
import { GmsModal } from '../../shared/ui/dialog/dialog';
import { GmsInput } from '../../shared/ui/field/field';
import { GmsState } from '../../shared/ui/state/state';
import { ToastService } from '../../shared/ui/toast/toast';
import { STATUS_BADGES, PRIORITY_BADGES, RISK_BADGES, BadgeTone } from '../../shared/ui/badge/badge';
import {
  detailTimeline,
  detailActivity,
  detailDocuments,
  relatedLinks,
  pendingActions,
  upcomingSteps
} from './approval-vm';

type DecisionType = 'approve' | 'reject' | 'revision';

@Component({
  selector: 'app-approval-detail',
  imports: [
    DatePipe,
    GmsIcon,
    GmsPage,
    GmsPageHeader,
    GmsButton,
    GmsMenu,
    GmsTabs,
    GmsTimeline,
    GmsActivityFeed,
    GmsItemList,
    GmsRelationshipStrip,
    GmsContextSection,
    GmsModal,
    GmsInput,
    GmsState,
    TranslocoPipe
  ],
  providers: [provideTranslocoScope('approvals')],
  templateUrl: './approval-detail.html',
  styleUrl: './approval-detail.scss'
})
export class ApprovalDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly approvalService = inject(ApprovalService);
  private readonly recent = inject(RecentService);
  private readonly wsContext = inject(WorkspaceContextService);
  private readonly toast = inject(ToastService);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly approval = signal<Approval | null>(null);

  protected readonly statusLabelKey = computed(() => STATUS_BADGES[this.approval()?.status ?? '']?.labelKey ?? '');
  protected readonly statusLabel = computed(() => {
    this.language.current();
    return this.transloco.translate(this.statusLabelKey()) || (this.approval()?.status ?? '');
  });
  protected readonly statusTone = computed<BadgeTone>(() => STATUS_BADGES[this.approval()?.status ?? '']?.tone ?? 'neutral');
  protected readonly priorityLabel = computed(() => {
    this.language.current();
    const key = PRIORITY_BADGES[this.approval()?.priority ?? '']?.labelKey;
    return key ? this.transloco.translate(key) : (this.approval()?.priority ?? '');
  });
  protected readonly priorityTone = computed<BadgeTone>(() => PRIORITY_BADGES[this.approval()?.priority ?? '']?.tone ?? 'neutral');
  protected readonly riskLabel = computed(() => {
    this.language.current();
    const key = RISK_BADGES[this.approval()?.risk ?? '']?.labelKey;
    return key ? this.transloco.translate(key) : (this.approval()?.risk ?? '');
  });
  protected readonly riskTone = computed<BadgeTone>(() => RISK_BADGES[this.approval()?.risk ?? '']?.tone ?? 'neutral');

  /** Human decision label derived from status (currentDecision from the core service is a raw TR sentence we can't rely on for i18n). */
  protected readonly decisionLabel = computed(() => {
    this.language.current();
    const s = this.approval()?.status;
    const key =
      s === 'Approved' ? 'approvals.decision.approved'
      : s === 'Rejected' ? 'approvals.decision.rejected'
      : s === 'Expired' ? 'approvals.decision.expired'
      : s === 'Waiting' ? 'approvals.decision.waiting'
      : 'approvals.decision.pending';
    return this.transloco.translate(key);
  });

  protected readonly isPending = computed(() => this.approval()?.status === 'Pending');

  protected readonly headerContext = computed(() => {
    this.language.current();
    const a = this.approval();
    if (!a) return [];
    const tr = (k: string) => this.transloco.translate(k);
    const ctx = [
      { key: tr('approvals.relationType.release'), label: a.releaseCode },
      { key: tr('approvals.decisionBar.approverLabel'), label: a.currentApprover }
    ];
    if (a.changeCode) ctx.splice(1, 0, { key: tr('approvals.relationType.change'), label: a.changeCode });
    return ctx;
  });

  protected readonly breadcrumbs = computed(() => {
    this.language.current();
    const tr = (k: string) => this.transloco.translate(k);
    return [
      { label: tr('approvals.breadcrumbWorkspace') },
      { label: tr('approvals.moduleName'), route: '/approvals' },
      { label: this.approval()?.code ?? tr('approvals.detail.fallbackTitle') }
    ];
  });

  protected readonly relationChain = computed<RelationNode[]>(() => {
    this.language.current();
    const a = this.approval();
    if (!a) return [];
    const tr = (k: string) => this.transloco.translate(k);
    const chain: RelationNode[] = [
      { type: tr('approvals.relationType.customer'), label: a.customerName, icon: 'briefcase', route: '/employees' },
      { type: tr('approvals.relationType.project'), label: a.projectName, icon: 'folder', route: '/releases' },
      { type: tr('approvals.relationType.release'), label: a.releaseCode, icon: 'release', route: `/releases/${a.releaseId}` }
    ];
    if (a.changeId && a.changeCode) {
      chain.push({ type: tr('approvals.relationType.change'), label: a.changeCode, icon: 'change', route: `/changes/${a.changeId}` });
    }
    chain.push({ type: tr('approvals.relationType.approval'), label: a.code, icon: 'approval', current: true });
    return chain;
  });

  // Tabs
  protected readonly activeTab = signal('overview');
  protected readonly tabs = computed<TabItem[]>(() => {
    this.language.current();
    const tr = (k: string) => this.transloco.translate(k);
    return [
      { id: 'overview', label: tr('approvals.tabs.overview'), icon: 'dashboard' },
      { id: 'flow', label: tr('approvals.tabs.flow'), icon: 'approval' },
      { id: 'comments', label: tr('approvals.tabs.comments'), icon: 'inbox' },
      { id: 'attachments', label: tr('approvals.tabs.attachments'), icon: 'document' },
      { id: 'timeline', label: tr('approvals.tabs.timeline'), icon: 'clock' },
      { id: 'audit', label: tr('approvals.tabs.audit'), icon: 'audit' }
    ];
  });
  protected readonly activeTabMeta = computed(() => this.tabs().find((tab) => tab.id === this.activeTab()) ?? this.tabs()[0]);

  protected readonly extraActions = computed(() => {
    this.language.current();
    const tr = (k: string) => this.transloco.translate(k);
    return [
      { label: tr('approvals.extra.delegate'), value: 'delegate', icon: 'user' as const, disabled: true },
      { label: tr('approvals.extra.forward'), value: 'forward', icon: 'share' as const, disabled: true }
    ];
  });

  // Overview + panel data
  private readonly t = (key: string, params?: Record<string, unknown>) => this.transloco.translate(key, params);
  protected readonly timeline = computed(() => { this.language.current(); return this.approval() ? detailTimeline(this.approval()!, this.t) : []; });
  protected readonly activity = computed(() => { this.language.current(); return this.approval() ? detailActivity(this.approval()!, this.t) : []; });
  protected readonly documents = computed(() => detailDocuments());
  protected readonly related = computed(() => { this.language.current(); return this.approval() ? relatedLinks(this.approval()!, this.t) : []; });
  protected readonly pending = computed(() => { this.language.current(); return this.approval() ? pendingActions(this.approval()!, this.t) : []; });
  protected readonly upcoming = computed(() => (this.approval() ? upcomingSteps(this.approval()!) : []));
  protected readonly steps = computed<ApprovalStep[]>(() => this.approval()?.steps ?? []);

  // Decision modal
  protected readonly decisionOpen = signal(false);
  protected readonly decisionType = signal<DecisionType>('approve');
  protected readonly decisionComment = signal('');
  protected readonly submitting = signal(false);
  protected readonly decisionMeta = computed(() => {
    this.language.current();
    const tr = (k: string) => this.transloco.translate(k);
    switch (this.decisionType()) {
      case 'approve': return { title: tr('approvals.decisionModal.approveTitle'), confirm: tr('approvals.decisionModal.approveConfirm'), variant: 'primary' as const, hint: tr('approvals.decisionModal.approveHint') };
      case 'reject': return { title: tr('approvals.decisionModal.rejectTitle'), confirm: tr('approvals.decisionModal.rejectConfirm'), variant: 'destructive' as const, hint: tr('approvals.decisionModal.rejectHint') };
      default: return { title: tr('approvals.decisionModal.revisionTitle'), confirm: tr('approvals.decisionModal.revisionConfirm'), variant: 'secondary' as const, hint: tr('approvals.decisionModal.revisionHint') };
    }
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.approvalService.getApproval(id).subscribe({
      next: (approval) => {
        if (!approval) {
          this.notFound.set(true);
          this.loading.set(false);
          return;
        }
        this.approval.set(approval);
        this.loading.set(false);
        this.registerContext(approval);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
  }

  private registerContext(a: Approval): void {
    this.recent.add({ id: a.id, type: 'change', label: a.code, hint: a.title, route: `/approvals/${a.id}`, icon: 'approval' });
    this.wsContext.set({
      customer: { label: a.customerName },
      project: { label: a.projectName, route: '/releases' },
      environment: { label: a.environmentName },
      release: { label: a.releaseCode, route: `/releases/${a.releaseId}` },
      change: a.changeCode ? { label: a.changeCode, route: `/changes/${a.changeId}` } : null
    });
  }

  openDecision(type: DecisionType): void {
    this.decisionType.set(type);
    this.decisionComment.set('');
    this.decisionOpen.set(true);
  }

  confirmDecision(): void {
    const a = this.approval();
    if (!a) return;
    const comment = this.decisionComment().trim();
    const type = this.decisionType();
    this.submitting.set(true);

    const obs =
      type === 'approve' ? this.approvalService.approve(a.id, comment)
      : type === 'reject' ? this.approvalService.reject(a.id, comment)
      : this.approvalService.requestRevision(a.id, comment);

    obs.subscribe((updated) => {
      this.submitting.set(false);
      this.decisionOpen.set(false);
      if (updated) this.approval.set(updated);
      const savedTitle = this.transloco.translate('approvals.toastDecision.approvedTitle');
      if (type === 'approve') this.toast.success(this.transloco.translate('approvals.toastDecision.approved', { code: a.code }), savedTitle);
      else if (type === 'reject') this.toast.warning(this.transloco.translate('approvals.toastDecision.rejected', { code: a.code }), savedTitle);
      else this.toast.info(this.transloco.translate('approvals.toastDecision.revision', { code: a.code }));
    });
  }

  onExtraAction(action: string): void {
    this.toast.info(this.transloco.translate(action === 'delegate' ? 'approvals.extra.delegateSoon' : 'approvals.extra.forwardSoon'));
  }

  back(): void {
    this.router.navigate(['/approvals']);
  }

  stepIcon(status: StepStatus): IconName {
    switch (status) {
      case 'approved': return 'check';
      case 'rejected': return 'close';
      case 'pending': return 'clock';
      case 'completed': return 'shield';
      default: return 'clock';
    }
  }

  stepClass(status: StepStatus): string {
    return 'flow__node--' + status;
  }

  /** Translation key for a step's status label (approve-flow steps use a local enum, not the shared badge registry). */
  stepStatusKey(status: StepStatus): string {
    return 'approvals.flow.stepStatus.' + status;
  }
}
