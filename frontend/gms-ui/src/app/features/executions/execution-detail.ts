import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { ExecutionService, Execution, StepStatus, LogSeverity, runnerLabel } from '../../core/execution.service';
import { RecentService } from '../../core/recent.service';
import { WorkspaceContextService } from '../../core/workspace-context.service';
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
import { ConfirmService } from '../../shared/ui/dialog/dialog';
import { STATUS_BADGES, BadgeTone } from '../../shared/ui/badge/badge';
import {
  stepIcon, stepTone, stepLabel, severityTone, severityLabel,
  detailTimeline, detailActivity, detailDocuments, relatedLinks, upcomingSteps
} from './execution-vm';

@Component({
  selector: 'app-execution-detail',
  imports: [
    DatePipe, GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsMenu, GmsTabs,
    GmsTimeline, GmsActivityFeed, GmsItemList, GmsRelationshipStrip, GmsContextSection, GmsState
  ],
  templateUrl: './execution-detail.html',
  styleUrl: './execution-detail.scss'
})
export class ExecutionDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly executionService = inject(ExecutionService);
  private readonly recent = inject(RecentService);
  private readonly wsContext = inject(WorkspaceContextService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  protected readonly runnerLabel = runnerLabel;

  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly execution = signal<Execution | null>(null);

  protected readonly statusLabel = computed(() => STATUS_BADGES[this.execution()?.status ?? '']?.label ?? this.execution()?.status ?? '');
  protected readonly statusTone = computed<BadgeTone>(() => STATUS_BADGES[this.execution()?.status ?? '']?.tone ?? 'neutral');
  protected readonly riskLabel = computed(() => {
    const r = this.execution()?.risk;
    return r === 'High' ? 'Yüksek' : r === 'Medium' ? 'Orta' : r === 'Low' ? 'Düşük' : '';
  });
  protected readonly riskTone = computed<BadgeTone>(() => {
    const r = this.execution()?.risk;
    return r === 'High' ? 'danger' : r === 'Medium' ? 'warning' : 'success';
  });

  // Control availability by status.
  protected readonly canStart = computed(() => ['Waiting', 'Ready'].includes(this.execution()?.status ?? ''));
  protected readonly canPause = computed(() => this.execution()?.status === 'Running');
  protected readonly canResume = computed(() => this.execution()?.status === 'Paused');
  protected readonly canCancel = computed(() => ['Waiting', 'Ready', 'Running', 'Paused'].includes(this.execution()?.status ?? ''));
  protected readonly isTerminal = computed(() => ['Completed', 'Failed', 'Cancelled', 'RolledBack'].includes(this.execution()?.status ?? ''));

  protected readonly headerContext = computed(() => {
    const e = this.execution();
    if (!e) return [];
    return [
      { key: 'Yayın', label: e.releaseCode },
      { key: 'Ortam', label: e.environmentName },
      { key: 'Yürütücü', label: e.executor }
    ];
  });

  protected readonly breadcrumbs = computed(() => [
    { label: 'Çalışma Alanı' },
    { label: 'Yürütme Merkezi', route: '/executions' },
    { label: this.execution()?.code ?? 'Yürütme' }
  ]);

  protected readonly relationChain = computed<RelationNode[]>(() => {
    const e = this.execution();
    if (!e) return [];
    return [
      { type: 'Müşteri', label: e.customerName, icon: 'briefcase', route: '/employees' },
      { type: 'Proje', label: e.projectName, icon: 'folder', route: '/releases' },
      { type: 'Yayın', label: e.releaseCode, icon: 'release', route: `/releases/${e.releaseId}` },
      { type: 'Değişiklik', label: e.changeCode, icon: 'change', route: `/changes/${e.changeId}` },
      { type: 'Yürütme', label: e.code, icon: 'execution', current: true }
    ];
  });

  // Tabs
  protected readonly activeTab = signal('overview');
  protected readonly tabs: TabItem[] = [
    { id: 'overview', label: 'Genel Bakış', icon: 'dashboard' },
    { id: 'plan', label: 'Yürütme Planı', icon: 'orgchart' },
    { id: 'log', label: 'Yürütme Kaydı', icon: 'document' },
    { id: 'rollback', label: 'Geri Alma', icon: 'change' },
    { id: 'verification', label: 'Doğrulama', icon: 'shield' },
    { id: 'timeline', label: 'Zaman Çizelgesi', icon: 'clock' },
    { id: 'audit', label: 'Denetim', icon: 'audit' }
  ];
  protected readonly activeTabMeta = computed(() => this.tabs.find((t) => t.id === this.activeTab()) ?? this.tabs[0]);

  protected readonly extraActions = [
    { label: 'Geri Al (yakında)', value: 'rollback', icon: 'change' as const },
    { label: 'Doğrula (yakında)', value: 'verify', icon: 'shield' as const }
  ];

  // Overview + panel data
  protected readonly steps = computed(() => this.execution()?.steps ?? []);
  protected readonly log = computed(() => this.execution()?.log ?? []);
  protected readonly timeline = computed(() => (this.execution() ? detailTimeline(this.execution()!) : []));
  protected readonly activity = computed(() => (this.execution() ? detailActivity(this.execution()!) : []));
  protected readonly documents = computed(() => detailDocuments());
  protected readonly related = computed(() => (this.execution() ? relatedLinks(this.execution()!) : []));
  protected readonly upcoming = computed(() => (this.execution() ? upcomingSteps(this.execution()!) : []));

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.executionService.getExecution(id).subscribe({
      next: (e) => {
        if (!e) {
          this.notFound.set(true);
          this.loading.set(false);
          return;
        }
        this.execution.set(e);
        this.loading.set(false);
        this.registerContext(e);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
  }

  private registerContext(e: Execution): void {
    this.recent.add({ id: e.id, type: 'change', label: e.code, hint: `${e.releaseCode} · ${e.environmentName}`, route: `/executions/${e.id}`, icon: 'execution' });
    this.wsContext.set({
      customer: { label: e.customerName },
      project: { label: e.projectName, route: '/releases' },
      environment: { label: e.environmentName },
      release: { label: e.releaseCode, route: `/releases/${e.releaseId}` },
      change: { label: e.changeCode, route: `/changes/${e.changeId}` }
    });
  }

  onStart(): void {
    this.confirm.ask({ title: 'Yürütmeyi başlat', message: `${this.execution()?.code} yürütmesini başlatmak istediğinize emin misiniz? Adımlar kontrollü şekilde çalıştırılacaktır.`, confirmText: 'Başlat', variant: 'success' })
      .then((ok) => { if (ok) this.run(this.executionService.start(this.execution()!.id), 'Yürütme başlatıldı.'); });
  }
  onPause(): void {
    this.confirm.ask({ title: 'Yürütmeyi duraklat', message: 'Aktif adım tamamlandıktan sonra yürütme duraklatılacaktır.', confirmText: 'Duraklat', variant: 'warning' })
      .then((ok) => { if (ok) this.run(this.executionService.pause(this.execution()!.id), 'Yürütme duraklatıldı.'); });
  }
  onResume(): void {
    this.confirm.ask({ title: 'Yürütmeyi devam ettir', message: 'Yürütme kaldığı yerden devam edecektir.', confirmText: 'Devam Et', variant: 'success' })
      .then((ok) => { if (ok) this.run(this.executionService.resume(this.execution()!.id), 'Yürütme devam ettirildi.'); });
  }
  onCancel(): void {
    this.confirm.ask({ title: 'Yürütmeyi iptal et', message: 'Yürütme iptal edilecek ve kalan adımlar atlanacaktır. Bu işlem geri alınamaz.', confirmText: 'İptal Et', variant: 'danger' })
      .then((ok) => { if (ok) this.run(this.executionService.cancel(this.execution()!.id), 'Yürütme iptal edildi.'); });
  }

  onExtra(action: string): void {
    this.toast.info(action === 'rollback' ? 'Geri alma yakında.' : 'Doğrulama yakında.');
  }

  private run(obs: ReturnType<ExecutionService['start']>, message: string): void {
    obs.subscribe((updated) => {
      if (updated) this.execution.set(updated);
      this.toast.success(message, 'Durum güncellendi');
    });
  }

  viewChange(): void {
    const e = this.execution();
    if (e) this.router.navigate(['/changes', e.changeId]);
  }

  back(): void {
    this.router.navigate(['/executions']);
  }

  stepIcon(s: StepStatus): IconName { return stepIcon(s); }
  stepTone(s: StepStatus): BadgeTone { return stepTone(s); }
  stepLabel(s: StepStatus): string { return stepLabel(s); }
  sevTone(s: LogSeverity): BadgeTone { return severityTone(s); }
  sevLabel(s: LogSeverity): string { return severityLabel(s); }
}
