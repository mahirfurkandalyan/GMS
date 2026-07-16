import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { WorkflowService, GmsWorkflow, WorkflowNode, nodeMeta } from '../../core/workflow.service';
import { LanguageService } from '../../core/language.service';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPage } from '../../shared/ui/page/page';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsButton } from '../../shared/ui/button/button';
import { GmsMenu, MenuItem } from '../../shared/ui/menu/menu';
import { GmsTabs, TabItem } from '../../shared/ui/tabs/tabs';
import { GmsTimeline } from '../../shared/ui/timeline/timeline';
import { GmsContextSection } from '../../shared/ui/context-panel/context-section';
import { GmsRelationshipStrip, RelationNode } from '../../shared/ui/relationship-strip/relationship-strip';
import { GmsState } from '../../shared/ui/state/state';
import { BadgeTone, STATUS_BADGES } from '../../shared/ui/badge/badge';
import { detailTimeline, edgeGeometry, canvasSize, categoryLabelKey, nodeLabelKey, nodeDescKey, NODE_W, NODE_H } from './workflow-vm';

@Component({
  selector: 'app-workflow-detail',
  imports: [
    DatePipe, GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsMenu, GmsTabs,
    GmsTimeline, GmsContextSection, GmsRelationshipStrip, GmsState, TranslocoPipe
  ],
  providers: [provideTranslocoScope('workflows')],
  templateUrl: './workflow-detail.html',
  styleUrl: './workflow-detail.scss'
})
export class WorkflowDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly workflowService = inject(WorkflowService);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  protected readonly nodeW = NODE_W;
  protected readonly nodeH = NODE_H;
  /** Icon/tone are not translatable — still resolved from the core registry as-is. */
  protected readonly nodeMeta = nodeMeta;

  /** Category / node-type labels — translated locally since the raw meta lives in a core service. */
  protected readonly categoryLabel = (key: string): string => this.transloco.translate(categoryLabelKey(key));
  protected readonly nodeLabel = (type: string): string => this.transloco.translate(nodeLabelKey(type));
  protected readonly nodeDesc = (type: string): string => this.transloco.translate(nodeDescKey(type));

  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly workflow = signal<GmsWorkflow | null>(null);
  protected readonly selectedNodeId = signal<string | null>(null);

  protected readonly statusLabel = computed(() => {
    this.language.current();
    const status = this.workflow()?.status;
    if (!status) return '';
    const key = STATUS_BADGES[status]?.labelKey;
    return key ? this.transloco.translate(key) : status;
  });
  protected readonly statusTone = computed<BadgeTone>(() => STATUS_BADGES[this.workflow()?.status ?? '']?.tone ?? 'neutral');

  protected readonly headerContext = computed(() => {
    this.language.current();
    const w = this.workflow();
    if (!w) return [];
    const t = (k: string) => this.transloco.translate(k);
    return [
      { key: t('workflows.detail.overview.category'), label: this.categoryLabel(w.category) },
      { key: t('workflows.detail.overview.currentVersion'), label: w.version },
      { key: t('workflows.detail.overview.createdBy'), label: w.createdBy }
    ];
  });

  protected readonly breadcrumbs = computed(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { label: t('workflows.detail.breadcrumbManagement') },
      { label: t('workflows.list.pageTitle'), route: '/workflows' },
      { label: this.workflow()?.code ?? t('workflows.detail.breadcrumbFallback') }
    ];
  });

  protected readonly relationChain = computed<RelationNode[]>(() => {
    this.language.current();
    const w = this.workflow();
    if (!w) return [];
    const t = (k: string) => this.transloco.translate(k);
    return [
      { type: t('workflows.detail.relation.domainType'), label: t('workflows.detail.relation.domain'), icon: 'shield', route: '/hub' },
      { type: t('workflows.detail.relation.categoryType'), label: this.categoryLabel(w.category), icon: 'share' },
      { type: t('workflows.detail.relation.workflowType'), label: w.code, icon: 'share', current: true }
    ];
  });

  // Tabs — Overview + Designer implemented; rest premium placeholders.
  protected readonly activeTab = signal('overview');
  protected readonly tabs = computed<TabItem[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { id: 'overview', label: t('workflows.detail.tab.overview'), icon: 'dashboard' },
      { id: 'designer', label: t('workflows.detail.tab.designer'), icon: 'share' },
      { id: 'variables', label: t('workflows.detail.tab.variables'), icon: 'change' },
      { id: 'versions', label: t('workflows.detail.tab.versions'), icon: 'folder' },
      { id: 'timeline', label: t('workflows.detail.tab.timeline'), icon: 'clock' },
      { id: 'audit', label: t('workflows.detail.tab.audit'), icon: 'audit' }
    ];
  });
  protected readonly activeTabMeta = computed(() => this.tabs().find((t) => t.id === this.activeTab()) ?? this.tabs()[0]);

  protected readonly extraActions = computed<MenuItem[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { label: t('workflows.detail.extra.newVersion'), value: 'version', icon: 'change' as const },
      { label: t('workflows.detail.extra.export'), value: 'export', icon: 'document' as const },
      { label: t('workflows.detail.extra.archive'), value: 'archive', icon: 'folder' as const }
    ];
  });

  // Canvas
  protected readonly nodes = computed(() => this.workflow()?.nodes ?? []);
  protected readonly edges = computed(() => edgeGeometry(this.nodes(), this.workflow()?.edges ?? []));
  protected readonly canvas = computed(() => canvasSize(this.nodes()));
  protected readonly selectedNode = computed<WorkflowNode | null>(() => this.nodes().find((n) => n.id === this.selectedNodeId()) ?? null);

  protected readonly timeline = computed(() => {
    this.language.current();
    const w = this.workflow();
    return w ? detailTimeline(w, (k, p) => this.transloco.translate(k, p)) : [];
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.workflowService.getWorkflow(id).subscribe({
      next: (w) => {
        if (!w) { this.notFound.set(true); this.loading.set(false); return; }
        this.workflow.set(w);
        this.selectedNodeId.set(w.nodes.find((n) => n.type === 'start')?.id ?? w.nodes[0]?.id ?? null);
        this.loading.set(false);
      },
      error: () => { this.notFound.set(true); this.loading.set(false); }
    });
  }

  selectNode(id: string): void {
    this.selectedNodeId.set(id);
  }

  onExtra(): void { /* placeholder actions handled by toast in future */ }

  back(): void {
    this.router.navigate(['/workflows']);
  }
}
