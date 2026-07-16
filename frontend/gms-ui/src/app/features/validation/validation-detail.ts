import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { ValidationService, Validation, ValidationFinding, ValidationResult } from '../../core/validation.service';
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
import { GmsState } from '../../shared/ui/state/state';
import { ToastService } from '../../shared/ui/toast/toast';
import { STATUS_BADGES, PRIORITY_BADGES, BadgeTone } from '../../shared/ui/badge/badge';
import {
  resultIcon, resultTone, counts,
  detailTimeline, detailActivity, detailDocuments, relatedLinks, recentValidations, upcomingActions
} from './validation-vm';

@Component({
  selector: 'app-validation-detail',
  imports: [
    DatePipe, GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsMenu, GmsTabs,
    GmsTimeline, GmsActivityFeed, GmsItemList, GmsRelationshipStrip, GmsContextSection, GmsState,
  ],
  providers: [provideTranslocoScope('validation')],
  templateUrl: './validation-detail.html',
  styleUrl: './validation-detail.scss'
})
export class ValidationDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly validationService = inject(ValidationService);
  private readonly recent = inject(RecentService);
  private readonly wsContext = inject(WorkspaceContextService);
  private readonly toast = inject(ToastService);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly validation = signal<Validation | null>(null);

  protected readonly resultLabel = computed(() => {
    this.language.current();
    const key = STATUS_BADGES[this.validation()?.result ?? '']?.labelKey;
    return key ? this.transloco.translate(key) : '';
  });
  protected readonly resultTone = computed<BadgeTone>(() => resultTone(this.validation()?.result ?? 'Skipped'));
  protected readonly severityLabelText = computed(() => {
    this.language.current();
    const key = PRIORITY_BADGES[this.validation()?.severity ?? '']?.labelKey;
    return key ? this.transloco.translate(key) : (this.validation()?.severity ?? '');
  });
  protected readonly severityTone = computed<BadgeTone>(() => PRIORITY_BADGES[this.validation()?.severity ?? '']?.tone ?? 'neutral');

  protected readonly counts = computed(() => counts(this.validation()?.findings ?? []));

  // Verdict — the "can this continue?" intelligence layer.
  protected readonly verdict = computed(() => {
    this.language.current();
    const tr = (k: string) => this.transloco.translate(k);
    const r = this.validation()?.result;
    if (r === 'Failed') return { tone: 'danger' as BadgeTone, icon: 'close' as IconName, title: tr('validation.verdict.blocked'), canContinue: false };
    if (r === 'Warning') return { tone: 'warning' as BadgeTone, icon: 'clock' as IconName, title: tr('validation.verdict.warn'), canContinue: true };
    return { tone: 'success' as BadgeTone, icon: 'check' as IconName, title: tr('validation.verdict.ready'), canContinue: true };
  });

  protected readonly headerContext = computed(() => {
    this.language.current();
    const v = this.validation();
    if (!v) return [];
    const tr = (k: string) => this.transloco.translate(k);
    return [
      { key: tr('validation.relationType.change'), label: v.changeCode },
      { key: tr('validation.relationType.release'), label: v.releaseCode },
      { key: tr('validation.kv.environment'), label: v.environmentName }
    ];
  });

  protected readonly breadcrumbs = computed(() => {
    this.language.current();
    const tr = (k: string) => this.transloco.translate(k);
    return [
      { label: tr('validation.breadcrumbWorkspace') },
      { label: tr('validation.moduleName'), route: '/validation' },
      { label: this.validation()?.code ?? tr('validation.detail.fallbackTitle') }
    ];
  });

  protected readonly relationChain = computed<RelationNode[]>(() => {
    this.language.current();
    const v = this.validation();
    if (!v) return [];
    const tr = (k: string) => this.transloco.translate(k);
    return [
      { type: tr('validation.relationType.customer'), label: v.customerName, icon: 'briefcase', route: '/employees' },
      { type: tr('validation.relationType.project'), label: v.projectName, icon: 'folder', route: '/releases' },
      { type: tr('validation.relationType.release'), label: v.releaseCode, icon: 'release', route: `/releases/${v.releaseId}` },
      { type: tr('validation.relationType.change'), label: v.changeCode, icon: 'change', route: `/changes/${v.changeId}` },
      { type: tr('validation.relationType.validation'), label: v.code, icon: 'shield', current: true }
    ];
  });

  // Tabs
  protected readonly activeTab = signal('overview');
  protected readonly tabs = computed<TabItem[]>(() => {
    this.language.current();
    const tr = (k: string) => this.transloco.translate(k);
    return [
      { id: 'overview', label: tr('validation.tabs.overview'), icon: 'dashboard' },
      { id: 'findings', label: tr('validation.tabs.findings'), icon: 'shield' },
      { id: 'recommendations', label: tr('validation.tabs.recommendations'), icon: 'activity' },
      { id: 'related', label: tr('validation.tabs.related'), icon: 'folder' },
      { id: 'timeline', label: tr('validation.tabs.timeline'), icon: 'clock' },
      { id: 'audit', label: tr('validation.tabs.audit'), icon: 'audit' }
    ];
  });
  protected readonly activeTabMeta = computed(() => this.tabs().find((tab) => tab.id === this.activeTab()) ?? this.tabs()[0]);

  protected readonly extraActions = computed(() => {
    this.language.current();
    const tr = (k: string) => this.transloco.translate(k);
    return [
      { label: tr('validation.extra.exportReport'), value: 'export', icon: 'document' as const },
      { label: tr('validation.extra.rerun'), value: 'rerun', icon: 'shield' as const }
    ];
  });

  // Overview + panel data
  private readonly t = (key: string, params?: Record<string, unknown>) => this.transloco.translate(key, params);
  protected readonly findings = computed<ValidationFinding[]>(() => this.validation()?.findings ?? []);
  protected readonly timeline = computed(() => { this.language.current(); return this.validation() ? detailTimeline(this.validation()!, this.t) : []; });
  protected readonly activity = computed(() => { this.language.current(); return this.validation() ? detailActivity(this.validation()!, this.t) : []; });
  protected readonly documents = computed(() => detailDocuments());
  protected readonly related = computed(() => (this.validation() ? relatedLinks(this.validation()!, this.t) : []));
  protected readonly recent$ = computed(() => (this.validation() ? recentValidations(this.validation()!, this.t) : []));
  protected readonly upcoming = computed(() => (this.validation() ? upcomingActions(this.validation()!) : []));
  protected readonly recommendations = computed(() =>
    this.findings().filter((f) => f.result !== 'Passed' && f.result !== 'Skipped')
  );

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.validationService.getValidation(id).subscribe({
      next: (v) => {
        if (!v) {
          this.notFound.set(true);
          this.loading.set(false);
          return;
        }
        this.validation.set(v);
        this.loading.set(false);
        this.registerContext(v);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
  }

  private registerContext(v: Validation): void {
    this.recent.add({ id: v.id, type: 'change', label: v.code, hint: v.rule, route: `/validation/${v.id}`, icon: 'shield' });
    this.wsContext.set({
      customer: { label: v.customerName },
      project: { label: v.projectName, route: '/releases' },
      environment: { label: v.environmentName },
      release: { label: v.releaseCode, route: `/releases/${v.releaseId}` },
      change: { label: v.changeCode, route: `/changes/${v.changeId}` }
    });
  }

  resultLabelOf(result: ValidationResult): string {
    return STATUS_BADGES[result]?.label ?? result;
  }
  findingIcon(result: ValidationResult): IconName {
    return resultIcon(result);
  }
  findingTone(result: ValidationResult): BadgeTone {
    return resultTone(result);
  }
  severityText(s: string): string {
    const key = PRIORITY_BADGES[s]?.labelKey;
    return key ? this.transloco.translate(key) : s;
  }
  severityToneFor(s: string): BadgeTone {
    return PRIORITY_BADGES[s]?.tone ?? 'neutral';
  }

  onExtra(action: string): void {
    this.toast.info(action === 'export' ? 'Rapor dışa aktarma yakında.' : 'Yeniden doğrulama yakında.');
  }

  viewChange(): void {
    const v = this.validation();
    if (v) this.router.navigate(['/changes', v.changeId]);
  }

  back(): void {
    this.router.navigate(['/validation']);
  }
}
