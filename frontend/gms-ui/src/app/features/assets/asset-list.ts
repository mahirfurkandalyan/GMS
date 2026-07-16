import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { AuthService } from '../../core/auth.service';
import {
  AssetService, GmsAsset, ASSET_TYPES, ASSET_CRITICALITIES, ASSET_STATUSES, ASSET_ENVIRONMENTS,
  assetTypeIcon
} from '../../core/asset.service';
import { LanguageService } from '../../core/language.service';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPage } from '../../shared/ui/page/page';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsButton } from '../../shared/ui/button/button';
import { GmsField, GmsInput } from '../../shared/ui/field/field';
import { GmsModal, ConfirmService } from '../../shared/ui/dialog/dialog';
import { GmsFormSection } from '../../shared/ui/form-section/form-section';
import { GmsDataGrid, GmsCellDef, ColumnDef, RowActionEvent } from '../../shared/ui/data-grid/data-grid';
import { STANDARD_ROW_ACTIONS } from '../../shared/ui/data-grid/presets';
import { GmsState } from '../../shared/ui/state/state';
import { GmsContextSection } from '../../shared/ui/context-panel/context-section';
import { GmsRelationshipStrip, RelationNode } from '../../shared/ui/relationship-strip/relationship-strip';
import { GmsActivityFeed, ActivityItem } from '../../shared/ui/activity-feed/activity-feed';
import { GmsItemList, LinkItem } from '../../shared/ui/item-list/item-list';
import { ToastService } from '../../shared/ui/toast/toast';
import { STATUS_BADGES, PRIORITY_BADGES } from '../../shared/ui/badge/badge';
import { assetTypeLabel } from './asset-vm';

@Component({
  selector: 'app-asset-list',
  imports: [
    FormsModule, DatePipe, GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsField, GmsInput,
    GmsModal, GmsFormSection, GmsDataGrid, GmsCellDef, GmsState,
    GmsContextSection, GmsRelationshipStrip, GmsActivityFeed, GmsItemList, TranslocoPipe
  ],
  providers: [provideTranslocoScope('assets')],
  templateUrl: './asset-list.html',
  styleUrl: './asset-list.scss'
})
export class AssetList implements OnInit {
  private readonly assetService = inject(AssetService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  protected readonly assetTypes = ASSET_TYPES;
  protected readonly criticalities = ASSET_CRITICALITIES;
  protected readonly statuses = ASSET_STATUSES;
  protected readonly environments = ASSET_ENVIRONMENTS;
  protected readonly typeLabel = (key: string) => assetTypeLabel(key, this.transloco);
  protected readonly typeIcon = assetTypeIcon;
  protected readonly critLabel = (c: string) => {
    const meta = PRIORITY_BADGES[c];
    return meta?.labelKey ? this.transloco.translate(meta.labelKey) : c;
  };
  protected readonly statusLabel = (s: string) => {
    const meta = STATUS_BADGES[s];
    return meta?.labelKey ? this.transloco.translate(meta.labelKey) : s;
  };

  protected readonly rows = signal<GmsAsset[]>([]);
  protected readonly loading = signal(false);

  protected readonly fSearch = signal('');
  protected readonly fType = signal('');
  protected readonly fProject = signal('');
  protected readonly fEnv = signal('');
  protected readonly fCriticality = signal('');
  protected readonly fOwner = signal('');
  protected readonly fStatus = signal('');

  protected readonly projectOptions = computed(() => [...new Set(this.rows().map((r) => r.projectName))].sort());
  protected readonly ownerOptions = computed(() => [...new Set(this.rows().map((r) => r.owner))].sort());

  protected readonly filteredRows = computed(() => {
    const q = this.fSearch().trim().toLocaleLowerCase('tr');
    return this.rows().filter((a) => {
      const matchesText =
        !q ||
        a.code.toLocaleLowerCase('tr').includes(q) ||
        a.name.toLocaleLowerCase('tr').includes(q) ||
        a.owner.toLocaleLowerCase('tr').includes(q) ||
        a.projectName.toLocaleLowerCase('tr').includes(q);
      return (
        matchesText &&
        (!this.fType() || a.type === this.fType()) &&
        (!this.fProject() || a.projectName === this.fProject()) &&
        (!this.fEnv() || a.environment === this.fEnv()) &&
        (!this.fCriticality() || a.criticality === this.fCriticality()) &&
        (!this.fOwner() || a.owner === this.fOwner()) &&
        (!this.fStatus() || a.status === this.fStatus())
      );
    });
  });

  protected readonly hasFilters = computed(
    () => !!(this.fSearch() || this.fType() || this.fProject() || this.fEnv() || this.fCriticality() || this.fOwner() || this.fStatus())
  );

  protected readonly columns = computed<ColumnDef[]>(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { key: 'code', header: t.translate('assets.column.id'), sticky: true, sortable: true, width: '140px' },
      { key: 'name', header: t.translate('assets.column.name'), sortable: true },
      { key: 'type', header: t.translate('assets.column.type') },
      { key: 'projectName', header: t.translate('assets.column.project') },
      { key: 'environment', header: t.translate('assets.column.environment'), type: 'badge', badgeKind: 'environment' },
      { key: 'owner', header: t.translate('assets.column.owner') },
      { key: 'criticality', header: t.translate('assets.column.criticality'), type: 'badge', badgeKind: 'priority', sortable: true },
      { key: 'status', header: t.translate('assets.column.status'), type: 'badge', badgeKind: 'status', sortable: true },
      { key: 'updatedAt', header: t.translate('assets.column.updatedAt'), sortable: true },
      { key: 'releases', header: t.translate('assets.column.releases'), width: '130px' }
    ];
  });

  protected readonly rowActions = STANDARD_ROW_ACTIONS;

  protected readonly relationChain = computed<RelationNode[]>(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { type: t.translate('assets.relation.domain'), label: t.translate('assets.relation.governance'), icon: 'shield', route: '/hub' },
      { type: t.translate('assets.relation.module'), label: t.translate('assets.breadcrumbCenter'), icon: 'server', current: true }
    ];
  });
  protected readonly panelActivity = computed<ActivityItem[]>(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { actor: 'Ayşe Yılmaz', action: t.translate('assets.activity.updatedProdDatabase'), time: t.translate('assets.activity.twoHoursAgo'), icon: 'server' },
      { actor: 'Furkan Demir', action: t.translate('assets.activity.registeredMicroservice'), time: t.translate('assets.activity.fiveHoursAgo'), icon: 'hub' },
      { actor: 'Ali Vural', action: t.translate('assets.activity.deprecatedOldView'), time: t.translate('assets.activity.yesterday'), icon: 'change' }
    ];
  });
  protected readonly criticalAssets: LinkItem[] = [
    { id: 'ast-2026-001', label: 'AST-2026-001', hint: 'EBR Üretim Veritabanı · PROD', route: '/assets/ast-2026-001', icon: 'server' },
    { id: 'ast-2026-004', label: 'AST-2026-004', hint: 'EBR Batch API · PROD', route: '/assets/ast-2026-004', icon: 'share' },
    { id: 'ast-2026-008', label: 'AST-2026-008', hint: 'Kimlik Doğrulama Konf. · PROD', route: '/assets/ast-2026-008', icon: 'filter' }
  ];

  // Create form
  protected readonly createOpen = signal(false);
  protected readonly createError = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly formName = signal('');
  protected readonly formType = signal('database');
  protected readonly formProject = signal('');
  protected readonly formEnv = signal('DEV');
  protected readonly formCriticality = signal('Medium');
  protected readonly formDescription = signal('');
  protected readonly canCreate = computed(() => !!this.formName().trim() && !!this.formType() && !!this.formProject().trim());

  ngOnInit(): void {
    this.load();
  }

  openAsset(row: GmsAsset): void {
    this.router.navigate(['/assets', row.id]);
  }

  resetFilters(): void {
    this.fSearch.set(''); this.fType.set(''); this.fProject.set(''); this.fEnv.set('');
    this.fCriticality.set(''); this.fOwner.set(''); this.fStatus.set('');
  }

  openCreate(): void {
    this.formName.set(''); this.formType.set('database'); this.formProject.set('');
    this.formEnv.set('DEV'); this.formCriticality.set('Medium'); this.formDescription.set('');
    this.createError.set(null);
    this.createOpen.set(true);
  }

  onCreate(): void {
    this.createError.set(null);
    if (!this.canCreate()) {
      this.createError.set(this.transloco.translate('assets.form.validationError'));
      return;
    }
    const owner = String(this.auth.getCurrentUser()?.fullName ?? this.transloco.translate('assets.unknownOwner'));
    this.submitting.set(true);
    this.assetService
      .create({
        name: this.formName().trim(),
        type: this.formType(),
        projectId: this.formProject().trim().toLocaleLowerCase('tr'),
        projectName: this.formProject().trim(),
        environment: this.formEnv(),
        owner,
        criticality: this.formCriticality(),
        description: this.formDescription().trim()
      })
      .subscribe({
        next: (created) => {
          this.submitting.set(false);
          this.createOpen.set(false);
          this.toast.success(this.transloco.translate('assets.toast.created', { code: created.code }), this.transloco.translate('assets.toast.savedTitle'));
          this.load();
        },
        error: () => {
          this.submitting.set(false);
          this.createError.set(this.transloco.translate('assets.toast.createFailed'));
        }
      });
  }

  onRowAction(event: RowActionEvent): void {
    const a = event.row as GmsAsset;
    const t = this.transloco;
    switch (event.action) {
      case 'open': this.openAsset(a); break;
      case 'edit': this.toast.info(t.translate('assets.toast.editSoon', { code: a.code })); break;
      case 'duplicate': this.toast.success(t.translate('assets.toast.duplicated', { code: a.code }), t.translate('assets.toast.duplicatedTitle')); break;
      case 'copy-link':
        navigator.clipboard?.writeText(`${location.origin}/assets/${a.id}`);
        this.toast.success(t.translate('assets.toast.linkCopied'));
        break;
      case 'archive': this.toast.undo(t.translate('assets.toast.archived', { code: a.code }), () => this.toast.info(t.translate('assets.toast.restored'))); break;
      case 'delete':
        this.confirm.ask({
          title: t.translate('assets.toast.deleteTitle'),
          message: t.translate('assets.toast.deleteMessage', { code: a.code }),
          confirmText: t.translate('assets.toast.deleteConfirm'),
          variant: 'danger'
        }).then((ok) => { if (ok) this.toast.undo(t.translate('assets.toast.deleted', { code: a.code }), () => this.toast.info(t.translate('assets.toast.deleteRestored'))); });
        break;
    }
  }

  onExport(): void {
    this.toast.info(this.transloco.translate('assets.toast.exportSoon'));
  }

  reload(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.assetService.getAssets().subscribe((a) => {
      this.rows.set(a);
      this.loading.set(false);
    });
  }
}
