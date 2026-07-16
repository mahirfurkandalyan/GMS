import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { WorkflowInstanceApiService } from '../../core/workflow/workflow-instance-api.service';
import { WorkflowInstanceDetail, WorkflowStepInstance } from '../../core/workflow/workflow.models';
import { workflowStepTypeLabelKey, workflowEventLabelKey } from '../../core/workflow/workflow-labels';
import { AuthStateService } from '../../core/auth/auth-state.service';
import { ApiError } from '../../core/api-error';
import { HasPermissionDirective } from '../../core/auth/has-permission.directive';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPage } from '../../shared/ui/page/page';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsButton } from '../../shared/ui/button/button';
import { GmsBadge } from '../../shared/ui/badge/badge';
import { GmsState } from '../../shared/ui/state/state';
import { GmsContextSection } from '../../shared/ui/context-panel/context-section';
import { ConfirmService } from '../../shared/ui/dialog/dialog';
import { ToastService } from '../../shared/ui/toast/toast';

/**
 * Workflow instance / task detail — GET /api/workflow-instances/{id}. Shows the instance status, the
 * related change, the step timeline and events, and hosts the task actions (complete / reject) plus
 * the authorized instance controls (pause / resume / cancel). The backend remains the assignment and
 * lifecycle authority; a 403 (e.g. wrong assignment) is surfaced without dropping the session.
 */
@Component({
  selector: 'app-workflow-instance-detail',
  imports: [
    DatePipe, FormsModule, TranslocoPipe, HasPermissionDirective,
    GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsBadge, GmsState, GmsContextSection
  ],
  providers: [provideTranslocoScope('workflows')],
  templateUrl: './workflow-instance-detail.html',
  styleUrl: './workflow-instance-detail.scss'
})
export class WorkflowInstanceDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(WorkflowInstanceApiService);
  private readonly auth = inject(AuthStateService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);
  private readonly transloco = inject(TranslocoService);
  private readonly t = (k: string, p?: Record<string, unknown>) => this.transloco.translate(k, p);

  protected readonly stepTypeKey = workflowStepTypeLabelKey;
  protected readonly eventKey = workflowEventLabelKey;

  protected readonly loading = signal(true);
  protected readonly error = signal<ApiError | null>(null);
  protected readonly instance = signal<WorkflowInstanceDetail | null>(null);
  protected readonly submitting = signal(false);

  // Action inputs
  protected readonly comment = signal('');
  protected readonly rejectMode = signal(false);
  protected readonly rejectError = signal(false);

  protected readonly activeStep = computed<WorkflowStepInstance | null>(() =>
    this.instance()?.steps.find((s) => s.status === 'Active') ?? null);

  /** Whether the current user is the assignee of the active step (UI hint; backend is authoritative). */
  protected readonly canActOnTask = computed(() => {
    const inst = this.instance();
    const step = this.activeStep();
    const user = this.auth.user();
    if (!inst || !step || !user || inst.status !== 'Waiting') return false;
    if (step.assignedUserId && step.assignedUserId === user.id) return true;
    if (!step.assignedUserId && step.assignedRole) return user.roles.includes(step.assignedRole);
    return false;
  });

  protected readonly isWaiting = computed(() => this.instance()?.status === 'Waiting');
  protected readonly isRunning = computed(() => this.instance()?.status === 'Running');
  protected readonly isActive = computed(() => {
    const s = this.instance()?.status;
    return s === 'Waiting' || s === 'Running' || s === 'Created';
  });

  protected readonly breadcrumbs = computed(() => [
    { label: 'Çalışma Alanı' },
    { label: this.t('workflows.tasks.breadcrumb'), route: '/tasks' },
    { label: this.instance()?.instanceNo ?? '—' }
  ]);

  ngOnInit(): void { this.load(); }

  private load(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.loading.set(true);
    this.error.set(null);
    this.api.getById(id).subscribe({
      next: (i) => { this.instance.set(i); this.loading.set(false); this.comment.set(''); this.rejectMode.set(false); },
      error: (e: ApiError) => { this.error.set(e); this.loading.set(false); }
    });
  }

  reload(): void { this.load(); }
  back(): void { this.router.navigate(['/tasks']); }

  openChange(): void {
    const inst = this.instance();
    if (inst && inst.triggerObjectType === 'ChangeRequest') {
      this.router.navigate(['/changes', inst.triggerObjectId]);
    }
  }

  /* ── Complete ── */
  async doComplete(): Promise<void> {
    if (this.submitting()) return;
    const ok = await this.confirm.ask({
      title: this.t('workflows.tasks.confirmCompleteTitle'),
      message: this.t('workflows.tasks.confirmCompleteText'),
      confirmText: this.t('workflows.tasks.complete'), variant: 'info'
    });
    if (!ok) return;
    const id = this.instance()!.id;
    this.submitting.set(true);
    this.api.completeTask(id, this.comment().trim() || undefined).subscribe({
      next: (i) => { this.submitting.set(false); this.instance.set(i); this.comment.set(''); this.toast.success(this.t('workflows.tasks.completed')); },
      error: (e: ApiError) => { this.submitting.set(false); this.toast.error(e.message, e.title); }
    });
  }

  /* ── Reject ── */
  startReject(): void { this.rejectMode.set(true); this.rejectError.set(false); }
  cancelReject(): void { this.rejectMode.set(false); this.rejectError.set(false); }

  async doReject(): Promise<void> {
    if (this.submitting()) return;
    const comment = this.comment().trim();
    if (!comment) { this.rejectError.set(true); return; }
    const ok = await this.confirm.ask({
      title: this.t('workflows.tasks.rejectTitle'),
      message: this.t('workflows.tasks.rejectConsequence'),
      confirmText: this.t('workflows.tasks.reject'), variant: 'danger', destructive: true
    });
    if (!ok) return;
    const id = this.instance()!.id;
    this.submitting.set(true);
    this.api.rejectTask(id, comment).subscribe({
      next: (i) => { this.submitting.set(false); this.instance.set(i); this.comment.set(''); this.rejectMode.set(false); this.toast.success(this.t('workflows.tasks.rejected')); },
      error: (e: ApiError) => { this.submitting.set(false); this.toast.error(e.message, e.title); }
    });
  }

  /* ── Instance controls ── */
  doPause(): void {
    const id = this.instance()!.id;
    this.submitting.set(true);
    this.api.pause(id).subscribe({
      next: (i) => { this.submitting.set(false); this.instance.set(i); this.toast.success(this.t('workflows.runtime.paused')); },
      error: (e: ApiError) => { this.submitting.set(false); this.toast.error(e.message, e.title); }
    });
  }

  doResume(): void {
    const id = this.instance()!.id;
    this.submitting.set(true);
    this.api.resume(id).subscribe({
      next: (i) => { this.submitting.set(false); this.instance.set(i); this.toast.success(this.t('workflows.runtime.resumed')); },
      error: (e: ApiError) => { this.submitting.set(false); this.toast.error(e.message, e.title); }
    });
  }

  async doCancel(): Promise<void> {
    if (this.submitting()) return;
    const ok = await this.confirm.ask({
      title: this.t('workflows.runtime.confirmCancelTitle'),
      message: this.t('workflows.runtime.confirmCancelText'),
      confirmText: this.t('workflows.runtime.cancel'), variant: 'danger', destructive: true
    });
    if (!ok) return;
    const inst = this.instance()!;
    this.submitting.set(true);
    this.api.cancel(inst.id, undefined, inst.rowVersion).subscribe({
      next: (i) => { this.submitting.set(false); this.instance.set(i); this.toast.success(this.t('workflows.runtime.cancelled')); },
      error: (e: ApiError) => { this.submitting.set(false); this.toast.error(e.message, e.title); }
    });
  }
}
