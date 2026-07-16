import { Crumb } from '../../shared/ui/breadcrumbs/breadcrumbs';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { AssetService, GmsAsset, assetTypeIcon } from '../../core/asset.service';
import { RecentService } from '../../core/recent.service';
import { WorkspaceContextService } from '../../core/workspace-context.service';
import { LanguageService } from '../../core/language.service';
import { GmsIcon } from '../../shared/icon/icon';
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
  detailTimeline, detailActivity, recentChanges, recentReleases, relatedDocuments, relationshipGroups, assetTypeLabel
} from './asset-vm';

@Component({
  selector: 'app-asset-detail',
  imports: [
    DatePipe, GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsMenu, GmsTabs,
    GmsTimeline, GmsActivityFeed, GmsItemList, GmsRelationshipStrip, GmsContextSection, GmsState, TranslocoPipe
  ],
  providers: [provideTranslocoScope('assets')],
  templateUrl: './asset-detail.html',
  styleUrl: './asset-detail.scss'
})
export class AssetDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly assetService = inject(AssetService);
  private readonly recent = inject(RecentService);
  private readonly wsContext = inject(WorkspaceContextService);
  private readonly toast = inject(ToastService);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  protected readonly typeLabel = (key: string) => assetTypeLabel(key, this.transloco);
  protected readonly typeIcon = assetTypeIcon;

  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly asset = signal<GmsAsset | null>(null);

  protected readonly statusLabel = computed(() => {
    this.language.current();
    const status = this.asset()?.status ?? '';
    const meta = STATUS_BADGES[status];
    return meta?.labelKey ? this.transloco.translate(meta.labelKey) : status;
  });
  protected readonly statusTone = computed<BadgeTone>(() => STATUS_BADGES[this.asset()?.status ?? '']?.tone ?? 'neutral');
  protected readonly critLabel = computed(() => {
    this.language.current();
    const crit = this.asset()?.criticality ?? '';
    const meta = PRIORITY_BADGES[crit];
    return meta?.labelKey ? this.transloco.translate(meta.labelKey) : crit;
  });
  protected readonly critTone = computed<BadgeTone>(() => PRIORITY_BADGES[this.asset()?.criticality ?? '']?.tone ?? 'neutral');

  protected readonly headerContext = computed(() => {
    this.language.current();
    const a = this.asset();
    if (!a) return [];
    const t = this.transloco;
    return [
      { key: t.translate('assets.detail.field.type'), label: this.typeLabel(a.type) },
      { key: t.translate('assets.detail.field.project'), label: a.projectName },
      { key: t.translate('assets.detail.field.environment'), label: a.environment },
      { key: t.translate('assets.detail.field.owner'), label: a.owner }
    ];
  });

  protected readonly breadcrumbs = computed<Crumb[]>(() => {
    this.language.current();
    return [
      { label: this.transloco.translate('assets.breadcrumbWorkspace') },
      { label: this.transloco.translate('assets.breadcrumbCenter'), route: '/assets' },
      { label: this.asset()?.code ?? this.transloco.translate('assets.pageTitleSingular') }
    ];
  });

  protected readonly relationChain = computed<RelationNode[]>(() => {
    this.language.current();
    const a = this.asset();
    if (!a) return [];
    const t = this.transloco;
    const chain: RelationNode[] = [
      { type: t.translate('assets.relation.project'), label: a.projectName, icon: 'folder', route: '/releases' },
      { type: t.translate('assets.relation.environment'), label: a.environment, icon: 'server' }
    ];
    if (a.releases[0]) chain.push({ type: t.translate('assets.relation.release'), label: a.releases[0].code, icon: 'release', route: a.releases[0].route });
    if (a.changes[0]) chain.push({ type: t.translate('assets.relation.change'), label: a.changes[0].code, icon: 'change', route: a.changes[0].route });
    chain.push({ type: t.translate('assets.relation.asset'), label: a.code, icon: 'server', current: true });
    return chain;
  });

  // Tabs — Overview + Relationships fully implemented; rest premium placeholders.
  protected readonly activeTab = signal('overview');
  protected readonly tabs = computed<TabItem[]>(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { id: 'overview', label: t.translate('assets.detail.tab.overview'), icon: 'dashboard' },
      { id: 'relationships', label: t.translate('assets.detail.tab.relationships'), icon: 'share' },
      { id: 'releases', label: t.translate('assets.detail.tab.releases'), icon: 'release' },
      { id: 'changes', label: t.translate('assets.detail.tab.changes'), icon: 'change' },
      { id: 'documents', label: t.translate('assets.detail.tab.documents'), icon: 'document' },
      { id: 'timeline', label: t.translate('assets.detail.tab.timeline'), icon: 'clock' },
      { id: 'audit', label: t.translate('assets.detail.tab.audit'), icon: 'audit' }
    ];
  });
  protected readonly activeTabMeta = computed(() => this.tabs().find((t) => t.id === this.activeTab()) ?? this.tabs()[0]);

  protected readonly extraActions = computed(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { label: t.translate('assets.detail.viewDependenciesSoon'), value: 'deps', icon: 'share' as const },
      { label: t.translate('assets.detail.impactAnalysisSoon'), value: 'impact', icon: 'activity' as const },
      { label: t.translate('assets.detail.archive'), value: 'archive', icon: 'folder' as const }
    ];
  });

  // Tab / panel data
  protected readonly groups = computed(() => { this.language.current(); return this.asset() ? relationshipGroups(this.asset()!, this.transloco) : []; });
  protected readonly timeline = computed(() => { this.language.current(); return this.asset() ? detailTimeline(this.asset()!, this.transloco) : []; });
  protected readonly activity = computed(() => { this.language.current(); return this.asset() ? detailActivity(this.asset()!, this.transloco) : []; });
  protected readonly changes$ = computed(() => (this.asset() ? recentChanges(this.asset()!) : []));
  protected readonly releases$ = computed(() => (this.asset() ? recentReleases(this.asset()!) : []));
  protected readonly documents$ = computed(() => (this.asset() ? relatedDocuments(this.asset()!) : []));

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.assetService.getAsset(id).subscribe({
      next: (a) => {
        if (!a) {
          this.notFound.set(true);
          this.loading.set(false);
          return;
        }
        this.asset.set(a);
        this.loading.set(false);
        this.registerContext(a);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
  }

  private registerContext(a: GmsAsset): void {
    this.recent.add({ id: a.id, type: 'asset', label: a.code, hint: a.name, route: `/assets/${a.id}`, icon: 'server' });
    this.wsContext.set({
      customer: null,
      project: { label: a.projectName, route: '/releases' },
      release: a.releases[0] ? { label: a.releases[0].code, route: a.releases[0].route } : null,
      change: a.changes[0] ? { label: a.changes[0].code, route: a.changes[0].route } : null
    });
  }

  onEdit(): void {
    this.toast.info(this.transloco.translate('assets.detail.editSoon'));
  }
  onExtra(action: string): void {
    if (action === 'archive') this.toast.undo(this.transloco.translate('assets.detail.archivedToast', { code: this.asset()?.code }), () => this.toast.info(this.transloco.translate('assets.detail.restoredToast')));
    else this.toast.info(this.transloco.translate('assets.detail.featureSoon'));
  }

  back(): void {
    this.router.navigate(['/assets']);
  }
}
