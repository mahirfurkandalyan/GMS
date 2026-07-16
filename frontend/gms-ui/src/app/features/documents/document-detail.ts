import { Crumb } from '../../shared/ui/breadcrumbs/breadcrumbs';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { DocumentService, GmsDocument, docTypeIcon } from '../../core/document.service';
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
import { GmsDataGrid, ColumnDef } from '../../shared/ui/data-grid/data-grid';
import { GmsState } from '../../shared/ui/state/state';
import { ToastService } from '../../shared/ui/toast/toast';
import { STATUS_BADGES, BadgeTone } from '../../shared/ui/badge/badge';
import { detailTimeline, detailActivity, relatedLinks, recentDocuments, previewCode, previewText, documentTypeLabel } from './document-vm';

@Component({
  selector: 'app-document-detail',
  imports: [
    DatePipe, GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsMenu, GmsTabs,
    GmsTimeline, GmsActivityFeed, GmsItemList, GmsRelationshipStrip, GmsContextSection, GmsDataGrid, GmsState, TranslocoPipe
  ],
  providers: [provideTranslocoScope('documents')],
  templateUrl: './document-detail.html',
  styleUrl: './document-detail.scss'
})
export class DocumentDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly documentService = inject(DocumentService);
  private readonly recent = inject(RecentService);
  private readonly wsContext = inject(WorkspaceContextService);
  private readonly toast = inject(ToastService);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  protected readonly typeIcon = docTypeIcon;
  protected readonly typeLabel = (key: string) => documentTypeLabel(key, this.transloco);

  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly document = signal<GmsDocument | null>(null);

  protected readonly statusLabel = computed(() => {
    this.language.current();
    const status = this.document()?.status ?? '';
    const meta = STATUS_BADGES[status];
    return meta?.labelKey ? this.transloco.translate(meta.labelKey) : status;
  });
  protected readonly statusTone = computed<BadgeTone>(() => STATUS_BADGES[this.document()?.status ?? '']?.tone ?? 'neutral');

  protected readonly headerContext = computed(() => {
    this.language.current();
    const d = this.document();
    if (!d) return [];
    return [
      { key: this.transloco.translate('documents.detail.field.category'), label: this.typeLabel(d.category) },
      { key: this.transloco.translate('documents.detail.field.version'), label: d.version },
      { key: this.transloco.translate('documents.detail.field.owner'), label: d.owner }
    ];
  });

  protected readonly breadcrumbs = computed<Crumb[]>(() => {
    this.language.current();
    return [
      { label: this.transloco.translate('documents.breadcrumbWorkspace') },
      { label: this.transloco.translate('documents.breadcrumbCenter'), route: '/documents' },
      { label: this.document()?.code ?? this.transloco.translate('documents.pageTitleSingular') }
    ];
  });

  protected readonly relationChain = computed<RelationNode[]>(() => {
    this.language.current();
    const d = this.document();
    if (!d) return [];
    const t = this.transloco;
    const chain: RelationNode[] = [
      { type: t.translate('documents.relation.customer'), label: d.customerName, icon: 'briefcase', route: '/employees' },
      { type: t.translate('documents.relation.project'), label: d.projectName, icon: 'folder', route: '/releases' }
    ];
    if (d.releaseId && d.releaseCode) chain.push({ type: t.translate('documents.relation.release'), label: d.releaseCode, icon: 'release', route: `/releases/${d.releaseId}` });
    if (d.changeId && d.changeCode) chain.push({ type: t.translate('documents.relation.change'), label: d.changeCode, icon: 'change', route: `/changes/${d.changeId}` });
    chain.push({ type: t.translate('documents.relation.document'), label: d.code, icon: 'document', current: true });
    return chain;
  });

  // Tabs
  protected readonly activeTab = signal('overview');
  protected readonly tabs = computed<TabItem[]>(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { id: 'overview', label: t.translate('documents.detail.tab.overview'), icon: 'dashboard' },
      { id: 'versions', label: t.translate('documents.detail.tab.versions'), icon: 'change' },
      { id: 'related', label: t.translate('documents.detail.tab.related'), icon: 'folder' },
      { id: 'preview', label: t.translate('documents.detail.tab.preview'), icon: 'search' },
      { id: 'timeline', label: t.translate('documents.detail.tab.timeline'), icon: 'clock' },
      { id: 'audit', label: t.translate('documents.detail.tab.audit'), icon: 'audit' }
    ];
  });
  protected readonly activeTabMeta = computed(() => this.tabs().find((t) => t.id === this.activeTab()) ?? this.tabs()[0]);

  protected readonly extraActions = computed(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { label: t.translate('documents.detail.downloadSoon'), value: 'download', icon: 'document' as const },
      { label: t.translate('documents.detail.archive'), value: 'archive', icon: 'folder' as const }
    ];
  });

  // Overview + panel data
  protected readonly versions = computed(() => this.document()?.versions ?? []);
  protected readonly timeline = computed(() => { this.language.current(); return this.document() ? detailTimeline(this.document()!, this.transloco) : []; });
  protected readonly activity = computed(() => { this.language.current(); return this.document() ? detailActivity(this.document()!, this.transloco) : []; });
  protected readonly related = computed(() => { this.language.current(); return this.document() ? relatedLinks(this.document()!, this.transloco) : []; });
  protected readonly recent$ = computed(() => (this.document() ? recentDocuments(this.document()!) : []));
  protected readonly previewCode = computed(() => (this.document() ? previewCode(this.document()!) : ''));
  protected readonly previewText = computed(() => { this.language.current(); return this.document() ? previewText(this.document()!, this.transloco) : []; });

  protected readonly versionColumns = computed<ColumnDef[]>(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { key: 'version', header: t.translate('documents.detail.field.version'), sticky: true, width: '110px' },
      { key: 'createdBy', header: t.translate('documents.detail.field.createdBy') },
      { key: 'createdAt', header: t.translate('documents.detail.field.createdAt'), type: 'date' },
      { key: 'description', header: t.translate('documents.detail.field.description') },
      { key: 'status', header: t.translate('documents.detail.field.status'), type: 'badge', badgeKind: 'status' }
    ];
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.documentService.getDocument(id).subscribe({
      next: (d) => {
        if (!d) {
          this.notFound.set(true);
          this.loading.set(false);
          return;
        }
        this.document.set(d);
        this.loading.set(false);
        this.registerContext(d);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
  }

  private registerContext(d: GmsDocument): void {
    this.recent.add({ id: d.id, type: 'document', label: d.code, hint: d.name, route: `/documents/${d.id}`, icon: 'document' });
    this.wsContext.set({
      customer: { label: d.customerName },
      project: { label: d.projectName, route: '/releases' },
      release: d.releaseCode ? { label: d.releaseCode, route: `/releases/${d.releaseId}` } : null,
      change: d.changeCode ? { label: d.changeCode, route: `/changes/${d.changeId}` } : null
    });
  }

  onNewVersion(): void {
    this.toast.info(this.transloco.translate('documents.detail.newVersionSoon'));
  }
  onExtra(action: string): void {
    if (action === 'download') this.toast.info(this.transloco.translate('documents.detail.downloadSoonToast'));
    else this.toast.undo(this.transloco.translate('documents.detail.archivedToast', { code: this.document()?.code }), () => this.toast.info(this.transloco.translate('documents.detail.restoredToast')));
  }

  back(): void {
    this.router.navigate(['/documents']);
  }
}
