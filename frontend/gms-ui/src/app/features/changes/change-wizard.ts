import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import {
  CHANGE_CLASSES, CHANGE_TYPES, ChangeClass, RiskLevel, AffectedAssetRef,
  changeClassLabel, changeTypeLabel
} from '../../core/change.service';
import { ChangeApiService } from '../../core/change/change-api.service';
import { CreateChangeRequestInput } from '../../core/change/change.models';
import { KEBAB_TO_CHANGE_TYPE } from '../../core/change/change-labels';
import { ReferenceDataApiService, CustomerRef, ProjectRef, EnvironmentRef } from '../../core/reference/reference-data-api.service';
import { ApiError } from '../../core/api-error';
import { AssetService, GmsAsset, assetTypeLabel } from '../../core/asset.service';
import { DOCUMENT_TYPES, docTypeLabel } from '../../core/document.service';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPage } from '../../shared/ui/page/page';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsButton } from '../../shared/ui/button/button';
import { GmsField, GmsInput } from '../../shared/ui/field/field';
import { GmsFormSection } from '../../shared/ui/form-section/form-section';
import { GmsContextSection } from '../../shared/ui/context-panel/context-section';
import { GmsItemList, LinkItem } from '../../shared/ui/item-list/item-list';
import { ConfirmService } from '../../shared/ui/dialog/dialog';
import { ToastService } from '../../shared/ui/toast/toast';
import { PRIORITY_BADGES } from '../../shared/ui/badge/badge';
import { calculateRisk, RISK_META, technicalFields, TechFieldDef } from './change-vm';
import { TranslocoService } from '@jsverse/transloco';

interface WizardStep { n: number; key: string; title: string; icon: string; }
interface ReadinessCheck { label: string; ok: boolean; kind: 'required' | 'warning'; }

/** UI draft only (unsaved form recovery) — NOT domain persistence. Cleared on successful create. */
const DRAFT_KEY = 'gms.change.draft';

/**
 * Create Change wizard — the existing multi-step design, now saving through the real backend
 * (POST /api/change-requests, then optional submit). Reference data (customer → project → environment)
 * comes from the live API as a dependent chain. Risk shown here is a NON-authoritative preview; the
 * backend recalculates the official risk/readiness on create (shown on the real detail page).
 */
@Component({
  selector: 'app-change-wizard',
  imports: [
    FormsModule, GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsField, GmsInput,
    GmsFormSection, GmsContextSection, GmsItemList
  ],
  templateUrl: './change-wizard.html',
  styleUrl: './change-wizard.scss'
})
export class ChangeWizard implements OnInit {
  private readonly api = inject(ChangeApiService);
  private readonly ref = inject(ReferenceDataApiService);
  private readonly assetService = inject(AssetService);
  private readonly confirm = inject(ConfirmService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  private readonly t = (k: string, p?: Record<string, unknown>) => this.transloco.translate(k, p);

  protected readonly classes = CHANGE_CLASSES;
  protected readonly types = CHANGE_TYPES;
  protected readonly priorities: string[] = ['Critical', 'High', 'Medium', 'Low'];
  protected readonly docCategories = DOCUMENT_TYPES.filter((d) =>
    ['sql-script', 'rollback-script', 'release-note', 'test-evidence', 'architecture-doc', 'operational-procedure'].includes(d.key));
  protected readonly priorityLabel = (p: string) => PRIORITY_BADGES[p]?.label ?? p;
  protected readonly changeClassLabel = changeClassLabel;
  protected readonly changeTypeLabel = changeTypeLabel;
  protected readonly assetTypeLabel = assetTypeLabel;
  protected readonly docTypeLabel = docTypeLabel;

  protected readonly steps: WizardStep[] = [
    { n: 1, key: 'general', title: 'Genel Bilgiler', icon: 'change' },
    { n: 2, key: 'technical', title: 'Teknik Detaylar', icon: 'server' },
    { n: 3, key: 'assets', title: 'Etkilenen Varlıklar', icon: 'folder' },
    { n: 4, key: 'documents', title: 'Dokümanlar', icon: 'document' },
    { n: 5, key: 'precheck', title: 'Ön Kontrol', icon: 'shield' },
    { n: 6, key: 'review', title: 'Gözden Geçir & Gönder', icon: 'approval' }
  ];
  protected readonly step = signal(1);
  protected readonly currentStep = computed(() => this.steps[this.step() - 1]);

  // ── Reference data (dependent chain) ──
  protected readonly customers = signal<CustomerRef[]>([]);
  protected readonly projects = signal<ProjectRef[]>([]);
  protected readonly environments = signal<EnvironmentRef[]>([]);

  // Step 1 (IDs sent to the backend; names kept for preview/display)
  protected readonly title = signal('');
  protected readonly businessReason = signal('');
  protected readonly customerId = signal('');
  protected readonly projectId = signal('');
  protected readonly environmentId = signal('');
  protected readonly changeClass = signal<ChangeClass>('Normal');
  protected readonly changeType = signal('app-deploy'); // internal kebab key → mapped to backend on save
  protected readonly priority = signal('Medium');
  protected readonly plannedDate = signal('');
  protected readonly description = signal('');
  protected readonly sourceSystem = signal('');
  protected readonly sourceReference = signal('');

  protected readonly customerName = computed(() => this.customers().find((c) => c.id === this.customerId())?.name ?? '');
  protected readonly projectName = computed(() => this.projects().find((p) => p.id === this.projectId())?.name ?? '');
  protected readonly environmentName = computed(() => this.environments().find((e) => e.id === this.environmentId())?.name ?? '');

  // Step 2 dynamic technical values
  protected readonly technical = signal<Record<string, string | boolean>>({});
  protected readonly techDefs = computed<TechFieldDef[]>(() => technicalFields(this.changeType(), this.t));

  // Step 3 assets
  protected readonly allAssets = signal<GmsAsset[]>([]);
  protected readonly assetSearch = signal('');
  protected readonly assets = signal<AffectedAssetRef[]>([]);
  protected readonly filteredAssets = computed(() => {
    const q = this.assetSearch().trim().toLocaleLowerCase('tr');
    return this.allAssets().filter((a) =>
      !q || a.name.toLocaleLowerCase('tr').includes(q) || a.code.toLocaleLowerCase('tr').includes(q));
  });

  // Step 4 documents
  protected readonly docs = signal<string[]>([]);

  // Non-authoritative risk PREVIEW (backend recalculates on create)
  protected readonly risk = computed<RiskLevel>(() =>
    calculateRisk(this.environmentName(), this.changeClass(), this.changeType(), this.assets()));
  protected readonly riskMeta = computed(() => RISK_META[this.risk()]);
  protected readonly riskLabel = computed(() => this.t(RISK_META[this.risk()].labelKey));

  protected readonly criticalAssetCount = computed(() => this.assets().filter((a) => a.criticality === 'Critical').length);
  protected readonly impactEnvironments = computed(() => [...new Set(this.assets().map((a) => a.environment))]);
  protected readonly estimatedDowntime = computed(() => {
    const t = this.technical();
    if (t['downtimeExpected'] || t['maintenanceWindow'] || t['restartRequired']) return 'Bakım penceresi gerekiyor';
    if (this.criticalAssetCount() > 0 && this.environmentName() === 'PROD') return 'Kısa kesinti olası';
    return 'Beklenmiyor';
  });

  protected readonly step1Complete = computed(() =>
    !!this.title().trim() && !!this.businessReason().trim() && !!this.customerId() && !!this.projectId()
    && !!this.environmentId() && !!this.changeClass() && !!this.changeType() && !!this.priority() && !!this.plannedDate());

  protected stepComplete(n: number): boolean {
    switch (n) {
      case 1: return this.step1Complete();
      case 3: return this.assets().length > 0;
      case 4: return this.docs().length > 0;
      default: return this.step() > n;
    }
  }

  // Readiness preview (step 5) — UI only
  protected readonly needsRollback = computed(() => ['sql-fix', 'db-schema', 'sp-func'].includes(this.changeType()));
  protected readonly hasRollback = computed(() => {
    const t = this.technical();
    return this.docs().includes('rollback-script') || !!t['rollbackScript'] || !!t['rollbackPlan'];
  });
  protected readonly checks = computed<ReadinessCheck[]>(() => {
    const c: ReadinessCheck[] = [
      { label: 'İş gerekçesi girildi', ok: !!this.businessReason().trim(), kind: 'required' },
      { label: 'Ortam seçildi', ok: !!this.environmentId(), kind: 'required' },
      { label: 'Planlanan uygulama tarihi girildi', ok: !!this.plannedDate(), kind: 'required' },
      { label: 'En az bir etkilenen varlık eklendi', ok: this.assets().length > 0, kind: 'warning' },
      { label: 'En az bir destekleyici doküman seçildi', ok: this.docs().length > 0, kind: 'warning' }
    ];
    if (this.needsRollback()) {
      c.push({ label: 'Geri alma betiği / planı tanımlandı', ok: this.hasRollback(), kind: 'required' });
    }
    return c;
  });
  protected readonly readinessScore = computed(() => {
    const list = this.checks();
    if (!list.length) return 100;
    return Math.round((list.filter((x) => x.ok).length / list.length) * 100);
  });
  protected readonly warnings = computed(() => this.checks().filter((c) => !c.ok));
  protected readonly readinessTone = computed(() => {
    const s = this.readinessScore();
    return s >= 90 ? 'success' : s >= 60 ? 'warning' : 'danger';
  });

  protected readonly tips = computed<string[]>(() => {
    switch (this.currentStep().key) {
      case 'general': return ['Başlık kısa ve eylem odaklı olmalı.', 'İş gerekçesi onaycılar için kritiktir.', 'Buradaki risk yalnızca ön izlemedir; resmî risk backend tarafından hesaplanır.'];
      case 'technical': return ['Alanlar değişiklik türüne göre uyarlanır.', 'Geri alma bilgisini eksiksiz doldurun.'];
      case 'assets': return ['Kritik varlıklar riski yükseltir.', 'Etki özeti sağda güncellenir.'];
      case 'documents': return ['Kaynak kod yüklenmez; yalnızca kanıt seçilir.', 'Riskli değişikliklerde geri alma betiği ekleyin.'];
      case 'precheck': return ['Bu hazırlık göstergesi ön izlemedir.', 'Resmî hazırlık kontrolü gönderimde backend tarafından yapılır.'];
      default: return ['Göndermeden önce tüm bölümleri gözden geçirin.', 'Gönderim sonrası değişiklik incelemeye alınır.'];
    }
  });
  protected readonly recentSimilar = signal<LinkItem[]>([]);
  protected readonly relatedDocs: LinkItem[] = [
    { id: 'd1', label: 'Değişiklik Yönetimi SOP', hint: 'Prosedür', route: '/documents', icon: 'document' },
    { id: 'd2', label: 'Risk Değerlendirme Kılavuzu', hint: 'Kılavuz', route: '/documents', icon: 'document' }
  ];

  protected readonly submitting = signal(false);

  constructor() {
    effect(() => {
      const snapshot = this.snapshot();
      try { localStorage.setItem(DRAFT_KEY, JSON.stringify(snapshot)); } catch { /* ignore */ }
    });
  }

  ngOnInit(): void {
    this.restore();
    this.ref.customers().subscribe({ next: (c) => this.customers.set(c), error: () => this.customers.set([]) });
    if (this.customerId()) this.loadProjects(this.customerId());
    if (this.projectId()) this.loadEnvironments(this.projectId());
    this.assetService.getAssets().subscribe((a) => this.allAssets.set(a));
    this.api.list({ pageSize: 4, sortBy: 'createdAt', sortDir: 'desc' }).subscribe({
      next: (res) => this.recentSimilar.set(
        res.items.map((c): LinkItem => ({ id: c.id, label: c.changeNo, hint: c.title, route: `/changes/${c.id}`, icon: 'change' }))),
      error: () => this.recentSimilar.set([])
    });
  }

  // ── Navigation ──
  goStep(n: number): void { if (n >= 1 && n <= 6) this.step.set(n); }
  next(): void { if (this.step() < 6) this.step.set(this.step() + 1); }
  prev(): void { if (this.step() > 1) this.step.set(this.step() - 1); }

  // ── Reference data (dependent chain) ──
  onCustomer(id: string): void {
    this.customerId.set(id);
    this.projectId.set(''); this.environmentId.set('');
    this.projects.set([]); this.environments.set([]);
    if (id) this.loadProjects(id);
  }
  onProject(id: string): void {
    this.projectId.set(id);
    this.environmentId.set('');
    this.environments.set([]);
    if (id) this.loadEnvironments(id);
  }
  onEnvironment(id: string): void { this.environmentId.set(id); }

  private loadProjects(customerId: string): void {
    this.ref.projects(customerId).subscribe({ next: (p) => this.projects.set(p), error: () => this.projects.set([]) });
  }
  private loadEnvironments(projectId: string): void {
    this.ref.environments(projectId).subscribe({ next: (e) => this.environments.set(e), error: () => this.environments.set([]) });
  }

  // ── Technical ──
  getTech(key: string): string { const v = this.technical()[key]; return typeof v === 'string' ? v : ''; }
  getTechBool(key: string): boolean { return this.technical()[key] === true; }
  setTech(key: string, value: string | boolean): void { this.technical.update((t) => ({ ...t, [key]: value })); }
  toggleTech(key: string): void { this.setTech(key, !this.getTechBool(key)); }

  // ── Assets ──
  isAssetSelected(id: string): boolean { return this.assets().some((a) => a.id === id); }
  toggleAsset(a: GmsAsset): void {
    if (this.isAssetSelected(a.id)) {
      this.assets.update((list) => list.filter((x) => x.id !== a.id));
    } else {
      this.assets.update((list) => [...list, { id: a.id, code: a.code, name: a.name, type: a.type, criticality: a.criticality, environment: a.environment }]);
    }
  }

  // ── Documents ──
  isDocSelected(category: string): boolean { return this.docs().includes(category); }
  toggleDoc(category: string): void {
    this.docs.update((list) => list.includes(category) ? list.filter((x) => x !== category) : [...list, category]);
  }

  // ── UI draft persistence (form recovery only) ──
  private snapshot() {
    return {
      step: this.step(), title: this.title(), businessReason: this.businessReason(),
      customerId: this.customerId(), projectId: this.projectId(), environmentId: this.environmentId(),
      changeClass: this.changeClass(), changeType: this.changeType(), priority: this.priority(),
      plannedDate: this.plannedDate(), description: this.description(), sourceSystem: this.sourceSystem(),
      sourceReference: this.sourceReference(), technical: this.technical(), assets: this.assets(), docs: this.docs()
    };
  }
  private restore(): void {
    try {
      const raw = localStorage.getItem(DRAFT_KEY);
      if (!raw) return;
      const s = JSON.parse(raw);
      this.title.set(s.title ?? ''); this.businessReason.set(s.businessReason ?? '');
      this.customerId.set(s.customerId ?? ''); this.projectId.set(s.projectId ?? '');
      this.environmentId.set(s.environmentId ?? ''); this.changeClass.set(s.changeClass ?? 'Normal');
      this.changeType.set(s.changeType ?? 'app-deploy'); this.priority.set(s.priority ?? 'Medium');
      this.plannedDate.set(s.plannedDate ?? ''); this.description.set(s.description ?? '');
      this.sourceSystem.set(s.sourceSystem ?? ''); this.sourceReference.set(s.sourceReference ?? '');
      this.technical.set(s.technical ?? {}); this.assets.set(s.assets ?? []); this.docs.set(s.docs ?? []);
      this.step.set(s.step ?? 1);
    } catch { /* ignore */ }
  }
  private clearDraft(): void { try { localStorage.removeItem(DRAFT_KEY); } catch { /* ignore */ } }

  /** Build the backend Create DTO — maps UI values (kebab type, technical map) to backend constants. */
  private buildInput(): CreateChangeRequestInput {
    const t = this.technical();
    const summaryLines = Object.entries(t)
      .filter(([k]) => !['sqlScript', 'rollbackScript', 'rollbackPlan', 'technicalSummary'].includes(k))
      .map(([k, v]) => `${k}: ${typeof v === 'boolean' ? (v ? 'Evet' : 'Hayır') : v}`);
    return {
      title: this.title().trim(),
      description: this.description().trim(),
      businessReason: this.businessReason().trim(),
      customerId: this.customerId(),
      projectId: this.projectId(),
      environmentId: this.environmentId(),
      changeClass: this.changeClass(),
      changeType: KEBAB_TO_CHANGE_TYPE[this.changeType()] ?? 'Other',
      priority: this.priority(),
      plannedImplementationDate: this.plannedDate() ? new Date(this.plannedDate()).toISOString() : null,
      sourceSystem: this.sourceSystem().trim() || undefined,
      sourceReference: this.sourceReference().trim() || undefined,
      revision: {
        technicalSummary: (typeof t['technicalSummary'] === 'string' ? t['technicalSummary'] : '') || summaryLines.join('\n'),
        implementationNotes: this.description().trim(),
        deploymentInstructions: '',
        sqlScript: typeof t['sqlScript'] === 'string' ? t['sqlScript'] : '',
        rollbackScript: typeof t['rollbackScript'] === 'string' ? t['rollbackScript'] : '',
        rollbackStrategy: typeof t['rollbackPlan'] === 'string' ? (t['rollbackPlan'] as string) : '',
        rollbackOwner: '',
        estimatedDurationMinutes: 0
      },
      assets: this.assets().map((a) => ({ assetType: a.type, assetName: a.name, criticality: a.criticality, description: '' })),
      documents: this.docs().map((category) => ({ documentType: category, documentName: this.docTypeLabel(category), version: '', status: '' }))
    };
  }

  // ── Bottom actions ──
  saveDraft(): void {
    if (!this.guardStep1()) return;
    this.submitting.set(true);
    this.api.create(this.buildInput()).subscribe({
      next: (created) => {
        this.submitting.set(false);
        this.clearDraft();
        this.toast.success(`${created.changeNo} taslak olarak kaydedildi.`, 'Taslak kaydedildi');
        this.router.navigate(['/changes', created.id]);
      },
      error: (e: ApiError) => { this.submitting.set(false); this.toast.error(e.message, e.title); }
    });
  }

  submitForReview(): void {
    if (!this.guardStep1()) return;
    this.confirm.ask({
      title: 'İncelemeye gönder',
      message: `"${this.title()}" değişikliği oluşturulup incelemeye gönderilecek. Resmî risk ve hazırlık backend tarafından hesaplanır. Devam edilsin mi?`,
      confirmText: 'İncelemeye Gönder',
      variant: 'info'
    }).then((ok) => {
      if (!ok) return;
      this.submitting.set(true);
      // Create (Draft) → then submit. Both land on the real detail page.
      this.api.create(this.buildInput()).subscribe({
        next: (created) => {
          this.clearDraft();
          this.api.submit(created.id).subscribe({
            next: () => {
              this.submitting.set(false);
              this.toast.success(`${created.changeNo} incelemeye gönderildi.`, 'Gönderildi');
              this.router.navigate(['/changes', created.id]);
            },
            error: (e: ApiError) => {
              this.submitting.set(false);
              // Change exists as Draft; readiness/validation issues are resolved on the detail page.
              if (e.readinessFindings?.length) {
                this.toast.warning(`${created.changeNo} taslak olarak oluşturuldu ancak kritik hazırlık bulguları nedeniyle gönderilemedi. Detaydan giderin.`);
              } else {
                this.toast.error(e.message, e.title);
              }
              this.router.navigate(['/changes', created.id]);
            }
          });
        },
        error: (e: ApiError) => { this.submitting.set(false); this.toast.error(e.message, e.title); }
      });
    });
  }

  private guardStep1(): boolean {
    if (!this.step1Complete()) {
      this.toast.warning('Lütfen önce Genel Bilgiler adımını tamamlayın.');
      this.step.set(1);
      return false;
    }
    return true;
  }

  cancel(): void {
    this.confirm.ask({
      title: 'Sihirbazdan çık',
      message: 'Girdiğiniz bilgiler taslak olarak saklanır ancak değişiklik oluşturulmaz. Çıkmak istediğinize emin misiniz?',
      confirmText: 'Çık', variant: 'danger'
    }).then((ok) => { if (ok) this.router.navigate(['/changes']); });
  }
}
