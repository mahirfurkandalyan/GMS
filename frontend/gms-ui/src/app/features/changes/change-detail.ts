import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { ChangeApiService } from '../../core/change/change-api.service';
import {
  ChangeRequestDetail, UpdateChangeRequestInput, CreateChangeRevisionInput
} from '../../core/change/change.models';
import { changeClassKey, changeTypeKey, changePriorityKey, changeAuditEventKey } from '../../core/change/change-labels';
import { WorkflowInstanceApiService } from '../../core/workflow/workflow-instance-api.service';
import { WorkflowInstanceDetail, WorkflowStepInstance } from '../../core/workflow/workflow.models';
import { workflowStatusLabelKey, workflowStepTypeLabelKey } from '../../core/workflow/workflow-labels';
import { ReferenceDataApiService, EnvironmentRef } from '../../core/reference/reference-data-api.service';
import { ApiError } from '../../core/api-error';
import { HasPermissionDirective } from '../../core/auth/has-permission.directive';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPage } from '../../shared/ui/page/page';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsButton } from '../../shared/ui/button/button';
import { GmsTabs, TabItem } from '../../shared/ui/tabs/tabs';
import { GmsBadge } from '../../shared/ui/badge/badge';
import { GmsContextSection } from '../../shared/ui/context-panel/context-section';
import { GmsState } from '../../shared/ui/state/state';
import { GmsDrawer } from '../../shared/ui/dialog/dialog';
import { ConfirmService } from '../../shared/ui/dialog/dialog';
import { ToastService } from '../../shared/ui/toast/toast';
import { LanguageService } from '../../core/language.service';
import { CHANGE_PRIORITY_VALUES, CHANGE_CLASS_VALUES, CHANGE_TYPE_VALUES } from '../../core/change/change-labels';

/**
 * Change detail — real GET /api/change-requests/{id}. Hosts the full lifecycle: overview (general
 * info + backend risk/readiness + latest revision + assets/documents), the related workflow instance
 * state, the audit timeline, and the permission-gated actions (edit / submit / cancel / new revision).
 * Every action reloads from the backend; local status is never mutated ahead of a server success.
 */
@Component({
  selector: 'app-change-detail',
  imports: [
    DatePipe, FormsModule, TranslocoPipe, HasPermissionDirective,
    GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsTabs, GmsBadge, GmsContextSection, GmsState, GmsDrawer
  ],
  providers: [provideTranslocoScope('changes'), provideTranslocoScope('workflows')],
  templateUrl: './change-detail.html',
  styleUrl: './change-detail.scss'
})
export class ChangeDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(ChangeApiService);
  private readonly workflowApi = inject(WorkflowInstanceApiService);
  private readonly ref = inject(ReferenceDataApiService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);
  private readonly t = (k: string, p?: Record<string, unknown>) => this.transloco.translate(k, p);

  // ── Label helpers ──
  protected readonly classLabelKey = changeClassKey;
  protected readonly typeLabelKey = changeTypeKey;
  protected readonly priorityLabelKey = changePriorityKey;
  protected readonly auditLabelKey = changeAuditEventKey;
  protected readonly wfStatusKey = workflowStatusLabelKey;
  protected readonly wfStepTypeKey = workflowStepTypeLabelKey;

  protected readonly classOptions = CHANGE_CLASS_VALUES;
  protected readonly typeOptions = CHANGE_TYPE_VALUES;
  protected readonly priorityOptions = CHANGE_PRIORITY_VALUES;

  // ── Load state ──
  protected readonly loading = signal(true);
  protected readonly error = signal<ApiError | null>(null);
  protected readonly change = signal<ChangeRequestDetail | null>(null);

  // ── Workflow state ──
  protected readonly workflow = signal<WorkflowInstanceDetail | null>(null);
  protected readonly workflowLoading = signal(false);

  // ── Action state ──
  protected readonly submitting = signal(false);
  protected readonly readinessBlock = signal<ApiError['readinessFindings'] | null>(null);

  // ── Drawers ──
  protected readonly editOpen = signal(false);
  protected readonly revisionOpen = signal(false);
  protected readonly environments = signal<EnvironmentRef[]>([]);

  // Edit form
  protected readonly eTitle = signal('');
  protected readonly eDescription = signal('');
  protected readonly eBusinessReason = signal('');
  protected readonly ePriority = signal('');
  protected readonly eClass = signal('');
  protected readonly eType = signal('');
  protected readonly eEnvironmentId = signal('');
  protected readonly ePlannedDate = signal('');
  protected readonly editError = signal<ApiError | null>(null);

  // Revision form
  protected readonly rTechnicalSummary = signal('');
  protected readonly rImplementationNotes = signal('');
  protected readonly rDeploymentInstructions = signal('');
  protected readonly rSqlScript = signal('');
  protected readonly rRollbackScript = signal('');
  protected readonly rRollbackStrategy = signal('');
  protected readonly rRollbackOwner = signal('');
  protected readonly rDuration = signal(0);
  protected readonly revisionError = signal<ApiError | null>(null);

  // ── Derived ──
  protected readonly isDraft = computed(() => this.change()?.status === 'Draft');
  protected readonly canCancel = computed(() => {
    const s = this.change()?.status;
    return !!s && s !== 'Cancelled' && s !== 'Implemented';
  });
  protected readonly canRevise = computed(() => {
    const s = this.change()?.status;
    return !!s && s !== 'Cancelled' && s !== 'Implemented';
  });
  protected readonly criticalFindings = computed(
    () => this.change()?.readiness.findings.filter((f) => f.severity === 'Critical') ?? []);
  protected readonly warningFindings = computed(
    () => this.change()?.readiness.findings.filter((f) => f.severity !== 'Critical') ?? []);

  protected readonly activeStep = computed<WorkflowStepInstance | null>(() => {
    const wf = this.workflow();
    if (!wf) return null;
    return wf.steps.find((s) => s.status === 'Active') ?? null;
  });

  protected readonly breadcrumbs = computed(() => [
    { label: 'Çalışma Alanı' },
    { label: 'Değişiklikler', route: '/changes' },
    { label: this.change()?.changeNo ?? 'Değişiklik' }
  ]);

  protected readonly activeTab = signal('overview');
  protected readonly tabs: TabItem[] = [
    { id: 'overview', label: 'Genel Bakış', icon: 'dashboard' },
    { id: 'revision', label: 'Revizyon', icon: 'document' },
    { id: 'workflow', label: 'İş Akışı', icon: 'approval' },
    { id: 'audit', label: 'Denetim', icon: 'audit' }
  ];

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.loading.set(true);
    this.error.set(null);
    this.api.getById(id).subscribe({
      next: (c) => {
        this.change.set(c);
        this.loading.set(false);
        this.loadWorkflow(c.id);
      },
      error: (e: ApiError) => { this.error.set(e); this.loading.set(false); }
    });
  }

  private loadWorkflow(changeId: string): void {
    this.workflowLoading.set(true);
    this.workflowApi.list({ triggerObjectId: changeId, sortBy: 'createdAt', sortDir: 'desc', pageSize: 1 }).subscribe({
      next: (res) => {
        const newest = res.items[0];
        if (!newest) { this.workflow.set(null); this.workflowLoading.set(false); return; }
        this.workflowApi.getById(newest.id).subscribe({
          next: (wf) => { this.workflow.set(wf); this.workflowLoading.set(false); },
          error: () => { this.workflow.set(null); this.workflowLoading.set(false); }
        });
      },
      error: () => { this.workflow.set(null); this.workflowLoading.set(false); }
    });
  }

  reload(): void { this.load(); }
  back(): void { this.router.navigate(['/changes']); }

  /* ── Submit ── */
  async doSubmit(): Promise<void> {
    if (this.submitting()) return;
    const ok = await this.confirm.ask({
      title: this.t('changes.actions.confirmSubmitTitle'),
      message: this.t('changes.actions.confirmSubmitText'),
      confirmText: this.t('changes.actions.submit'), variant: 'info'
    });
    if (!ok) return;
    const id = this.change()!.id;
    this.submitting.set(true);
    this.readinessBlock.set(null);
    this.api.submit(id).subscribe({
      next: () => { this.submitting.set(false); this.toast.success(this.t('changes.actions.submitted')); this.load(); },
      error: (e: ApiError) => {
        this.submitting.set(false);
        if (e.readinessFindings?.length) { this.readinessBlock.set(e.readinessFindings); this.activeTab.set('overview'); }
        else this.toast.error(e.message, e.title);
      }
    });
  }

  /* ── Cancel ── */
  async doCancel(): Promise<void> {
    if (this.submitting()) return;
    const ok = await this.confirm.ask({
      title: this.t('changes.actions.confirmCancelTitle'),
      message: this.t('changes.actions.confirmCancelText'),
      confirmText: this.t('changes.actions.cancel'), variant: 'danger', destructive: true
    });
    if (!ok) return;
    const id = this.change()!.id;
    this.submitting.set(true);
    this.api.cancel(id).subscribe({
      next: () => { this.submitting.set(false); this.toast.success(this.t('changes.actions.cancelled')); this.load(); },
      error: (e: ApiError) => { this.submitting.set(false); this.toast.error(e.message, e.title); }
    });
  }

  /* ── Edit ── */
  openEdit(): void {
    const c = this.change();
    if (!c) return;
    this.eTitle.set(c.title);
    this.eDescription.set(c.description);
    this.eBusinessReason.set(c.businessReason);
    this.ePriority.set(c.priority);
    this.eClass.set(c.changeClass);
    this.eType.set(c.changeType);
    this.eEnvironmentId.set(c.environmentId);
    this.ePlannedDate.set(c.plannedImplementationDate ? c.plannedImplementationDate.substring(0, 10) : '');
    this.editError.set(null);
    this.ref.environments(c.projectId).subscribe({ next: (e) => this.environments.set(e), error: () => this.environments.set([]) });
    this.editOpen.set(true);
  }

  saveEdit(): void {
    const c = this.change();
    if (!c || this.submitting()) return;
    const input: UpdateChangeRequestInput = {
      title: this.eTitle().trim(),
      description: this.eDescription().trim(),
      businessReason: this.eBusinessReason().trim(),
      priority: this.ePriority(),
      changeClass: this.eClass(),
      changeType: this.eType(),
      environmentId: this.eEnvironmentId() || undefined,
      plannedImplementationDate: this.ePlannedDate() ? new Date(this.ePlannedDate()).toISOString() : undefined,
      rowVersion: c.rowVersion
    };
    this.submitting.set(true);
    this.editError.set(null);
    this.api.update(c.id, input).subscribe({
      next: (updated) => {
        this.submitting.set(false);
        this.change.set(updated);
        this.editOpen.set(false);
        this.toast.success(this.t('changes.actions.updated'));
      },
      error: (e: ApiError) => { this.submitting.set(false); this.editError.set(e); }
    });
  }

  /** 409 recovery — reload the latest record so the user can re-apply their edit on fresh data. */
  reloadForConflict(): void {
    this.editOpen.set(false);
    this.load();
  }

  /* ── Revision ── */
  openRevision(): void {
    this.rTechnicalSummary.set(''); this.rImplementationNotes.set(''); this.rDeploymentInstructions.set('');
    this.rSqlScript.set(''); this.rRollbackScript.set(''); this.rRollbackStrategy.set('');
    this.rRollbackOwner.set(''); this.rDuration.set(0); this.revisionError.set(null);
    this.revisionOpen.set(true);
  }

  saveRevision(): void {
    const c = this.change();
    if (!c || this.submitting()) return;
    const input: CreateChangeRevisionInput = {
      technicalSummary: this.rTechnicalSummary().trim(),
      implementationNotes: this.rImplementationNotes().trim(),
      deploymentInstructions: this.rDeploymentInstructions().trim(),
      sqlScript: this.rSqlScript().trim(),
      rollbackScript: this.rRollbackScript().trim(),
      rollbackStrategy: this.rRollbackStrategy().trim(),
      rollbackOwner: this.rRollbackOwner().trim(),
      estimatedDurationMinutes: Number(this.rDuration()) || 0
    };
    this.submitting.set(true);
    this.revisionError.set(null);
    this.api.addRevision(c.id, input).subscribe({
      next: (updated) => {
        this.submitting.set(false);
        this.change.set(updated);
        this.revisionOpen.set(false);
        this.toast.success(this.t('changes.actions.revisionCreated'));
      },
      error: (e: ApiError) => { this.submitting.set(false); this.revisionError.set(e); }
    });
  }

  openWorkflow(): void {
    const wf = this.workflow();
    if (wf) this.router.navigate(['/workflow-instances', wf.id]);
  }
}
