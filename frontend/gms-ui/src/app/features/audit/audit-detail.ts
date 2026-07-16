import { Crumb } from '../../shared/ui/breadcrumbs/breadcrumbs';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { AuditService, AuditRecord } from '../../core/audit.service';
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
import { GmsDataGrid, GmsCellDef, ColumnDef } from '../../shared/ui/data-grid/data-grid';
import { GmsState } from '../../shared/ui/state/state';
import { BadgeTone } from '../../shared/ui/badge/badge';
import { detailTimeline, relatedLinks, relatedByKind, recentActivityFeed, localActionMeta, localResultMeta, localChangeTypeMeta, moduleLabel } from './audit-vm';

@Component({
  selector: 'app-audit-detail',
  imports: [
    DatePipe, GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsMenu, GmsTabs,
    GmsTimeline, GmsActivityFeed, GmsItemList, GmsRelationshipStrip, GmsContextSection, GmsDataGrid, GmsCellDef, GmsState
  ],
  providers: [provideTranslocoScope('audit')],
  templateUrl: './audit-detail.html',
  styleUrl: './audit-detail.scss'
})
export class AuditDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auditService = inject(AuditService);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  protected readonly actionMeta = (a: string) => localActionMeta(a, this.transloco);
  protected readonly resultMeta = (r: string) => localResultMeta(r, this.transloco);
  protected readonly changeTypeMeta = (ct: string) => localChangeTypeMeta(ct, this.transloco);
  protected readonly moduleLabel = (m: string) => moduleLabel(m, this.transloco);

  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly record = signal<AuditRecord | null>(null);
  private readonly all = signal<AuditRecord[]>([]);

  protected readonly statusLabel = computed(() => { this.language.current(); return localResultMeta(this.record()?.result ?? '', this.transloco).label; });
  protected readonly statusTone = computed<BadgeTone>(() => localResultMeta(this.record()?.result ?? '', this.transloco).tone);

  protected readonly headerContext = computed(() => {
    this.language.current();
    const r = this.record();
    if (!r) return [];
    const t = this.transloco;
    return [
      { key: t.translate('audit.detail.field.user'), label: r.user },
      { key: t.translate('audit.detail.field.module'), label: this.moduleLabel(r.module) },
      { key: t.translate('audit.detail.field.action'), label: this.actionMeta(r.action).label },
      { key: t.translate('audit.detail.field.environment'), label: r.environment }
    ];
  });

  protected readonly breadcrumbs = computed<Crumb[]>(() => {
    this.language.current();
    return [
      { label: this.transloco.translate('audit.breadcrumbCompliance') },
      { label: this.transloco.translate('audit.pageTitle'), route: '/audit' },
      { label: this.record()?.code ?? this.transloco.translate('audit.detail.recordFallback') }
    ];
  });

  protected readonly relationChain = computed<RelationNode[]>(() => {
    this.language.current();
    const r = this.record();
    if (!r) return [];
    const t = this.transloco;
    const chain: RelationNode[] = [
      { type: t.translate('audit.detail.field.module'), label: this.moduleLabel(r.module), icon: 'folder', route: '/audit' },
      { type: t.translate('audit.relation.object'), label: r.objectName, icon: 'document' }
    ];
    const rel = r.related.find((x) => x.kind === 'release');
    const chg = r.related.find((x) => x.kind === 'change');
    if (rel) chain.push({ type: t.translate('audit.relation.release'), label: rel.code, icon: 'release', route: rel.route });
    if (chg) chain.push({ type: t.translate('audit.relation.change'), label: chg.code, icon: 'change', route: chg.route });
    chain.push({ type: t.translate('audit.relation.record'), label: r.code, icon: 'audit', current: true });
    return chain;
  });

  // Tabs — Overview / Field Changes / Timeline / Related fully implemented; Comments placeholder.
  protected readonly activeTab = signal('overview');
  protected readonly tabs = computed<TabItem[]>(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { id: 'overview', label: t.translate('audit.detail.tab.overview'), icon: 'dashboard' },
      { id: 'fields', label: t.translate('audit.detail.tab.fields'), icon: 'change' },
      { id: 'timeline', label: t.translate('audit.detail.tab.timeline'), icon: 'clock' },
      { id: 'related', label: t.translate('audit.detail.tab.related'), icon: 'folder' },
      { id: 'comments', label: t.translate('audit.detail.tab.comments'), icon: 'inbox' }
    ];
  });
  protected readonly activeTabMeta = computed(() => this.tabs().find((t) => t.id === this.activeTab()) ?? this.tabs()[0]);

  protected readonly extraActions = computed(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { label: t.translate('audit.detail.exportRecordSoon'), value: 'export', icon: 'document' as const },
      { label: t.translate('audit.detail.forwardToSiemSoon'), value: 'siem', icon: 'shield' as const }
    ];
  });

  protected readonly fieldColumns = computed<ColumnDef[]>(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { key: 'field', header: t.translate('audit.detail.field.fieldName'), sticky: true, sortable: true, width: '180px' },
      { key: 'previous', header: t.translate('audit.column.previousValue') },
      { key: 'current', header: t.translate('audit.column.newValue') },
      { key: 'changeType', header: t.translate('audit.detail.field.changeType') }
    ];
  });

  protected readonly fields = computed(() => this.record()?.fields ?? []);
  protected readonly timeline = computed(() => { this.language.current(); return this.record() ? detailTimeline(this.record()!, this.transloco) : []; });
  protected readonly related = computed(() => (this.record() ? relatedLinks(this.record()!) : []));
  protected readonly relReleases = computed(() => (this.record() ? relatedByKind(this.record()!, 'release') : []));
  protected readonly relChanges = computed(() => (this.record() ? relatedByKind(this.record()!, 'change') : []));
  protected readonly relDocuments = computed(() => (this.record() ? relatedByKind(this.record()!, 'document') : []));
  protected readonly activity = computed(() => { this.language.current(); return this.record() ? recentActivityFeed(this.all(), this.record()!.id, this.transloco) : []; });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.auditService.getRecords().subscribe((list) => this.all.set(list));
    this.auditService.getRecord(id).subscribe({
      next: (r) => {
        if (!r) {
          this.notFound.set(true);
          this.loading.set(false);
          return;
        }
        this.record.set(r);
        this.loading.set(false);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
  }

  back(): void {
    this.router.navigate(['/audit']);
  }
}
