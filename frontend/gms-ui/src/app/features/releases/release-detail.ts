import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { ReleaseApiService } from '../../core/release/release-api.service';
import { ReleasePlanDetail, UpdateReleasePlanInput } from '../../core/release/release.models';
import { releaseTypeKey, releaseAuditEventKey, RELEASE_TYPE_VALUES, RELEASE_TERMINAL_STATUSES } from '../../core/release/release-labels';
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
import { GmsDrawer, ConfirmService } from '../../shared/ui/dialog/dialog';
import { ToastService } from '../../shared/ui/toast/toast';
import { LanguageService } from '../../core/language.service';

/**
 * Release detail — real GET /api/releases/{id}. Overview (general info + backend risk + deployment
 * summary), included changes, deployment plan, audit timeline (real actor names) and the
 * permission-gated actions (edit / schedule / cancel). Every action reloads from the backend; local
 * status is never mutated ahead of a server success. RowVersion drives 409 optimistic-concurrency UX.
 */
@Component({
  selector: 'app-release-detail',
  imports: [
    DatePipe, FormsModule, TranslocoPipe, HasPermissionDirective,
    GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsTabs, GmsBadge, GmsContextSection, GmsState, GmsDrawer
  ],
  providers: [provideTranslocoScope('releases')],
  templateUrl: './release-detail.html',
  styleUrl: './release-detail.scss'
})
export class ReleaseDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(ReleaseApiService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);
  private readonly t = (k: string, p?: Record<string, unknown>) => this.transloco.translate(k, p);

  protected readonly typeLabelKey = releaseTypeKey;
  protected readonly auditLabelKey = releaseAuditEventKey;
  protected readonly typeOptions = RELEASE_TYPE_VALUES;

  protected readonly loading = signal(true);
  protected readonly error = signal<ApiError | null>(null);
  protected readonly release = signal<ReleasePlanDetail | null>(null);
  protected readonly submitting = signal(false);

  // Edit drawer
  protected readonly editOpen = signal(false);
  protected readonly eName = signal('');
  protected readonly eVersion = signal('');
  protected readonly eType = signal('');
  protected readonly ePlannedStart = signal('');
  protected readonly ePlannedEnd = signal('');
  protected readonly eBusinessOwner = signal('');
  protected readonly eTechnicalOwner = signal('');
  protected readonly eDescription = signal('');
  protected readonly editError = signal<ApiError | null>(null);

  protected readonly isPlanned = computed(() => this.release()?.status === 'Planned');
  protected readonly isTerminal = computed(() => {
    const s = this.release()?.status;
    return !!s && RELEASE_TERMINAL_STATUSES.has(s);
  });
  protected readonly canCancel = computed(() => {
    const s = this.release()?.status;
    return !!s && s !== 'Completed' && s !== 'Accepted' && s !== 'Cancelled';
  });

  protected readonly breadcrumbs = computed(() => [
    { label: 'Çalışma Alanı' },
    { label: 'Yayınlar', route: '/releases' },
    { label: this.release()?.releaseNo ?? 'Yayın' }
  ]);

  protected readonly activeTab = signal('overview');
  protected readonly tabs: TabItem[] = [
    { id: 'overview', label: 'Genel Bakış', icon: 'dashboard' },
    { id: 'changes', label: 'Değişiklikler', icon: 'change' },
    { id: 'deployment', label: 'Dağıtım', icon: 'server' },
    { id: 'audit', label: 'Denetim', icon: 'audit' }
  ];

  ngOnInit(): void { this.load(); }

  private load(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.loading.set(true);
    this.error.set(null);
    this.api.getById(id).subscribe({
      next: (r) => { this.release.set(r); this.loading.set(false); },
      error: (e: ApiError) => { this.error.set(e); this.loading.set(false); }
    });
  }

  reload(): void { this.load(); }
  back(): void { this.router.navigate(['/releases']); }
  openChange(changeId: string): void { this.router.navigate(['/changes', changeId]); }

  /* ── Schedule ── */
  async doSchedule(): Promise<void> {
    if (this.submitting()) return;
    const ok = await this.confirm.ask({
      title: this.t('releases.actions.confirmScheduleTitle'),
      message: this.t('releases.actions.confirmScheduleText'),
      confirmText: this.t('releases.actions.schedule'), variant: 'info'
    });
    if (!ok) return;
    const id = this.release()!.id;
    this.submitting.set(true);
    this.api.schedule(id).subscribe({
      next: (r) => { this.submitting.set(false); this.release.set(r); this.toast.success(this.t('releases.actions.scheduled')); },
      error: (e: ApiError) => { this.submitting.set(false); this.toast.error(e.message, e.title); }
    });
  }

  /* ── Cancel ── */
  async doCancel(): Promise<void> {
    if (this.submitting()) return;
    const ok = await this.confirm.ask({
      title: this.t('releases.actions.confirmCancelTitle'),
      message: this.t('releases.actions.confirmCancelText'),
      confirmText: this.t('releases.actions.cancel'), variant: 'danger', destructive: true
    });
    if (!ok) return;
    const id = this.release()!.id;
    this.submitting.set(true);
    this.api.cancel(id).subscribe({
      next: (r) => { this.submitting.set(false); this.release.set(r); this.toast.success(this.t('releases.actions.cancelled')); },
      error: (e: ApiError) => { this.submitting.set(false); this.toast.error(e.message, e.title); }
    });
  }

  /* ── Edit ── */
  openEdit(): void {
    const r = this.release();
    if (!r) return;
    this.eName.set(r.name);
    this.eVersion.set(r.version);
    this.eType.set(r.releaseType);
    this.ePlannedStart.set(r.plannedDeploymentStart ? r.plannedDeploymentStart.substring(0, 10) : '');
    this.ePlannedEnd.set(r.plannedDeploymentEnd ? r.plannedDeploymentEnd.substring(0, 10) : '');
    this.eBusinessOwner.set(r.businessOwner);
    this.eTechnicalOwner.set(r.technicalOwner);
    this.eDescription.set(r.description);
    this.editError.set(null);
    this.editOpen.set(true);
  }

  saveEdit(): void {
    const r = this.release();
    if (!r || this.submitting()) return;
    const input: UpdateReleasePlanInput = {
      name: this.eName().trim(),
      version: this.eVersion().trim(),
      releaseType: this.eType(),
      plannedDeploymentStart: this.ePlannedStart() ? new Date(this.ePlannedStart()).toISOString() : undefined,
      plannedDeploymentEnd: this.ePlannedEnd() ? new Date(this.ePlannedEnd()).toISOString() : undefined,
      businessOwner: this.eBusinessOwner().trim(),
      technicalOwner: this.eTechnicalOwner().trim(),
      description: this.eDescription().trim(),
      rowVersion: r.rowVersion
    };
    this.submitting.set(true);
    this.editError.set(null);
    this.api.update(r.id, input).subscribe({
      next: (updated) => { this.submitting.set(false); this.release.set(updated); this.editOpen.set(false); this.toast.success(this.t('releases.actions.updated')); },
      error: (e: ApiError) => { this.submitting.set(false); this.editError.set(e); }
    });
  }

  /** 409 recovery — reload the latest record so the user can re-apply their edit on fresh data. */
  reloadForConflict(): void { this.editOpen.set(false); this.load(); }
}
