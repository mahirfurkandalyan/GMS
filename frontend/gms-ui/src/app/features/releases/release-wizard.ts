import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { provideTranslocoScope } from '@jsverse/transloco';
import { ReleaseApiService } from '../../core/release/release-api.service';
import { CreateReleasePlanInput } from '../../core/release/release.models';
import { RELEASE_TYPE_VALUES, releaseTypeKey } from '../../core/release/release-labels';
import { ChangeApiService } from '../../core/change/change-api.service';
import { ChangeRequestListItem } from '../../core/change/change.models';
import { ReferenceDataApiService, CustomerRef, ProjectRef, EnvironmentRef } from '../../core/reference/reference-data-api.service';
import { AuthStateService } from '../../core/auth/auth-state.service';
import { ApiError } from '../../core/api-error';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPage } from '../../shared/ui/page/page';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsButton } from '../../shared/ui/button/button';
import { GmsField, GmsInput } from '../../shared/ui/field/field';
import { GmsFormSection } from '../../shared/ui/form-section/form-section';
import { GmsContextSection } from '../../shared/ui/context-panel/context-section';
import { ConfirmService } from '../../shared/ui/dialog/dialog';
import { ToastService } from '../../shared/ui/toast/toast';
import { TranslocoPipe } from '@jsverse/transloco';

interface WizardStep { n: number; key: string; title: string; icon: string; }

/** UI draft only (unsaved form recovery) — NOT domain persistence. Cleared on successful create. */
const DRAFT_KEY = 'gms.release.draft';

/**
 * Create Release Planning wizard — the existing multi-step design, now saving through the real backend
 * (POST /api/releases). Reference data (customer → project → environment) comes from the live API as a
 * dependent chain; the change pool is the REAL backend-approved changes for that customer/project/
 * environment (only Approved changes are selectable — never arbitrary ones). No business rule (risk,
 * duration) is computed here; the backend recalculates and validates on create.
 */
@Component({
  selector: 'app-release-wizard',
  imports: [
    FormsModule, TranslocoPipe, GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsField, GmsInput,
    GmsFormSection, GmsContextSection
  ],
  providers: [provideTranslocoScope('releases')],
  templateUrl: './release-wizard.html',
  styleUrl: './release-wizard.scss'
})
export class ReleaseWizard implements OnInit {
  private readonly api = inject(ReleaseApiService);
  private readonly changeApi = inject(ChangeApiService);
  private readonly ref = inject(ReferenceDataApiService);
  private readonly auth = inject(AuthStateService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly releaseTypes = RELEASE_TYPE_VALUES;
  protected readonly releaseTypeKey = releaseTypeKey;

  protected readonly steps: WizardStep[] = [
    { n: 1, key: 'info', title: 'Bilgiler', icon: 'release' },
    { n: 2, key: 'changes', title: 'Değişiklikler', icon: 'change' },
    { n: 3, key: 'deployment', title: 'Dağıtım', icon: 'server' },
    { n: 4, key: 'review', title: 'Gözden Geçir & Oluştur', icon: 'approval' }
  ];
  protected readonly step = signal(1);
  protected readonly currentStep = computed(() => this.steps[this.step() - 1]);

  // ── Reference data (dependent chain) ──
  protected readonly customers = signal<CustomerRef[]>([]);
  protected readonly projects = signal<ProjectRef[]>([]);
  protected readonly environments = signal<EnvironmentRef[]>([]);

  // Step 1 (IDs sent to the backend)
  protected readonly name = signal('');
  protected readonly version = signal('');
  protected readonly customerId = signal('');
  protected readonly projectId = signal('');
  protected readonly environmentId = signal('');
  protected readonly releaseType = signal('Minor');
  protected readonly plannedStart = signal('');
  protected readonly plannedEnd = signal('');
  protected readonly description = signal('');
  protected readonly businessOwner = signal('');
  protected readonly technicalOwner = signal('');

  protected readonly customerName = computed(() => this.customers().find((c) => c.id === this.customerId())?.name ?? '');
  protected readonly projectName = computed(() => this.projects().find((p) => p.id === this.projectId())?.name ?? '');
  protected readonly environmentName = computed(() => this.environments().find((e) => e.id === this.environmentId())?.name ?? '');
  protected readonly releaseManagerName = computed(() => this.auth.user()?.fullName ?? '—');

  // Step 2 — approved-change pool (real backend) + ordered selection
  protected readonly pool = signal<ChangeRequestListItem[]>([]);
  protected readonly poolLoading = signal(false);
  protected readonly selectedIds = signal<string[]>([]);
  protected readonly aSearch = signal('');

  private byId(id: string): ChangeRequestListItem | undefined { return this.pool().find((c) => c.id === id); }
  protected readonly selectedChanges = computed(() =>
    this.selectedIds().map((id) => this.byId(id)).filter((c): c is ChangeRequestListItem => !!c));

  protected readonly available = computed(() => {
    const q = this.aSearch().trim().toLocaleLowerCase('tr');
    const sel = new Set(this.selectedIds());
    return this.pool().filter((c) =>
      !sel.has(c.id)
      && (!q || c.changeNo.toLocaleLowerCase('tr').includes(q) || c.title.toLocaleLowerCase('tr').includes(q)));
  });

  // Step 3 — deployment plan
  protected readonly deploymentStrategy = signal('');
  protected readonly communicationPlan = signal('');
  protected readonly rollbackStrategy = signal('');
  protected readonly downtimeExpected = signal(false);
  protected readonly estimatedDowntime = signal(0);
  protected readonly deploymentNotes = signal('');

  // ── Completeness ──
  protected readonly step1Complete = computed(() =>
    !!this.name().trim() && !!this.version().trim() && !!this.customerId() && !!this.projectId()
    && !!this.environmentId() && !!this.releaseType());
  protected readonly hasChanges = computed(() => this.selectedIds().length > 0);

  protected stepComplete(n: number): boolean {
    switch (n) {
      case 1: return this.step1Complete();
      case 2: return this.hasChanges();
      default: return this.step() > n;
    }
  }

  protected readonly submitting = signal(false);
  protected readonly createError = signal<ApiError | null>(null);

  constructor() {
    effect(() => {
      const snap = this.snapshot();
      try { localStorage.setItem(DRAFT_KEY, JSON.stringify(snap)); } catch { /* ignore */ }
    });
  }

  ngOnInit(): void {
    this.restore();
    this.ref.customers().subscribe({ next: (c) => this.customers.set(c), error: () => this.customers.set([]) });
    if (this.customerId()) this.loadProjects(this.customerId());
    if (this.projectId()) this.loadEnvironments(this.projectId());
    if (this.environmentId()) this.loadApprovedChanges();
  }

  // Navigation
  goStep(n: number): void { if (n >= 1 && n <= this.steps.length) this.step.set(n); }
  next(): void { if (this.step() < this.steps.length) this.step.set(this.step() + 1); }
  prev(): void { if (this.step() > 1) this.step.set(this.step() - 1); }

  // ── Reference data (dependent chain) — resetting the context clears the change selection ──
  onCustomer(id: string): void {
    this.customerId.set(id);
    this.projectId.set(''); this.environmentId.set('');
    this.projects.set([]); this.environments.set([]); this.pool.set([]); this.selectedIds.set([]);
    if (id) this.loadProjects(id);
  }
  onProject(id: string): void {
    this.projectId.set(id);
    this.environmentId.set('');
    this.environments.set([]); this.pool.set([]); this.selectedIds.set([]);
    if (id) this.loadEnvironments(id);
  }
  onEnvironment(id: string): void {
    this.environmentId.set(id);
    this.pool.set([]); this.selectedIds.set([]);
    if (id) this.loadApprovedChanges();
  }

  private loadProjects(customerId: string): void {
    this.ref.projects(customerId).subscribe({ next: (p) => this.projects.set(p), error: () => this.projects.set([]) });
  }
  private loadEnvironments(projectId: string): void {
    this.ref.environments(projectId).subscribe({ next: (e) => this.environments.set(e), error: () => this.environments.set([]) });
  }

  /** Only backend-Approved changes for the selected customer/project/environment are selectable. */
  private loadApprovedChanges(): void {
    const customerId = this.customerId(), projectId = this.projectId(), environmentId = this.environmentId();
    if (!customerId || !projectId || !environmentId) return;
    this.poolLoading.set(true);
    this.changeApi.list({ status: 'Approved', customerId, projectId, environmentId, pageSize: 100, sortBy: 'changeNo', sortDir: 'asc' }).subscribe({
      next: (res) => { this.pool.set(res.items); this.poolLoading.set(false); },
      error: () => { this.pool.set([]); this.poolLoading.set(false); }
    });
  }

  // Dual-list (duplicate-safe: `available` already excludes selected)
  add(id: string): void { if (!this.selectedIds().includes(id)) this.selectedIds.update((l) => [...l, id]); }
  remove(id: string): void { this.selectedIds.update((l) => l.filter((x) => x !== id)); }
  addAll(): void { this.selectedIds.update((l) => [...l, ...this.available().map((c) => c.id)]); }
  removeAll(): void { this.selectedIds.set([]); }
  moveUp(i: number): void { if (i > 0) this.selectedIds.update((l) => { const n = [...l]; [n[i - 1], n[i]] = [n[i], n[i - 1]]; return n; }); }
  moveDown(i: number): void { this.selectedIds.update((l) => { if (i >= l.length - 1) return l; const n = [...l]; [n[i + 1], n[i]] = [n[i], n[i + 1]]; return n; }); }

  toggleDowntime(): void { this.downtimeExpected.update((v) => !v); }

  // ── UI draft persistence (form recovery only) ──
  private snapshot() {
    return {
      step: this.step(), name: this.name(), version: this.version(), customerId: this.customerId(),
      projectId: this.projectId(), environmentId: this.environmentId(), releaseType: this.releaseType(),
      plannedStart: this.plannedStart(), plannedEnd: this.plannedEnd(), description: this.description(),
      businessOwner: this.businessOwner(), technicalOwner: this.technicalOwner(), selectedIds: this.selectedIds(),
      deploymentStrategy: this.deploymentStrategy(), communicationPlan: this.communicationPlan(),
      rollbackStrategy: this.rollbackStrategy(), downtimeExpected: this.downtimeExpected(),
      estimatedDowntime: this.estimatedDowntime(), deploymentNotes: this.deploymentNotes()
    };
  }
  private restore(): void {
    try {
      const raw = localStorage.getItem(DRAFT_KEY);
      if (!raw) return;
      const s = JSON.parse(raw);
      this.name.set(s.name ?? ''); this.version.set(s.version ?? ''); this.customerId.set(s.customerId ?? '');
      this.projectId.set(s.projectId ?? ''); this.environmentId.set(s.environmentId ?? ''); this.releaseType.set(s.releaseType ?? 'Minor');
      this.plannedStart.set(s.plannedStart ?? ''); this.plannedEnd.set(s.plannedEnd ?? ''); this.description.set(s.description ?? '');
      this.businessOwner.set(s.businessOwner ?? ''); this.technicalOwner.set(s.technicalOwner ?? ''); this.selectedIds.set(s.selectedIds ?? []);
      this.deploymentStrategy.set(s.deploymentStrategy ?? ''); this.communicationPlan.set(s.communicationPlan ?? '');
      this.rollbackStrategy.set(s.rollbackStrategy ?? ''); this.downtimeExpected.set(s.downtimeExpected ?? false);
      this.estimatedDowntime.set(s.estimatedDowntime ?? 0); this.deploymentNotes.set(s.deploymentNotes ?? '');
      this.step.set(s.step ?? 1);
    } catch { /* ignore */ }
  }
  private clearDraft(): void { try { localStorage.removeItem(DRAFT_KEY); } catch { /* ignore */ } }

  private buildInput(): CreateReleasePlanInput {
    const user = this.auth.user();
    return {
      name: this.name().trim(),
      version: this.version().trim(),
      customerId: this.customerId(),
      projectId: this.projectId(),
      environmentId: this.environmentId(),
      releaseType: this.releaseType(),
      plannedDeploymentStart: this.plannedStart() ? new Date(this.plannedStart()).toISOString() : null,
      plannedDeploymentEnd: this.plannedEnd() ? new Date(this.plannedEnd()).toISOString() : null,
      businessOwner: this.businessOwner().trim() || undefined,
      technicalOwner: this.technicalOwner().trim() || undefined,
      releaseManagerUserId: user?.id ?? '',
      description: this.description().trim() || undefined,
      changeIds: this.selectedIds(),
      deploymentPlan: {
        deploymentStrategy: this.deploymentStrategy().trim() || undefined,
        communicationPlan: this.communicationPlan().trim() || undefined,
        rollbackStrategy: this.rollbackStrategy().trim() || undefined,
        downtimeExpected: this.downtimeExpected(),
        estimatedDowntimeMinutes: Number(this.estimatedDowntime()) || 0,
        notes: this.deploymentNotes().trim() || undefined
      },
      documents: []
    };
  }

  private guard(): boolean {
    if (!this.step1Complete()) { this.toast.warning('Lütfen önce Bilgiler adımını tamamlayın.'); this.step.set(1); return false; }
    if (!this.hasChanges()) { this.toast.warning('Lütfen en az bir onaylı değişiklik seçin.'); this.step.set(2); return false; }
    return true;
  }

  createPlan(): void {
    if (!this.guard() || this.submitting()) return;
    this.confirm.ask({
      title: 'Yayın oluştur',
      message: `"${this.name()}" yayını ${this.selectedIds().length} onaylı değişiklikle oluşturulacak. Resmî risk ve süre backend tarafından hesaplanır. Devam edilsin mi?`,
      confirmText: 'Yayın Oluştur', variant: 'info'
    }).then((ok) => {
      if (!ok) return;
      this.submitting.set(true);
      this.createError.set(null);
      this.api.create(this.buildInput()).subscribe({
        next: (created) => {
          this.submitting.set(false);
          this.clearDraft();
          this.toast.success(`${created.releaseNo} oluşturuldu.`, 'Yayın oluşturuldu');
          this.router.navigate(['/releases', created.id]);
        },
        error: (e: ApiError) => { this.submitting.set(false); this.createError.set(e); this.toast.error(e.message, e.title); }
      });
    });
  }

  cancel(): void {
    this.confirm.ask({
      title: 'Sihirbazdan çık',
      message: 'Girdiğiniz bilgiler taslak olarak saklanır ancak yayın oluşturulmaz. Çıkmak istediğinize emin misiniz?',
      confirmText: 'Çık', variant: 'danger'
    }).then((ok) => { if (ok) this.router.navigate(['/releases']); });
  }
}
