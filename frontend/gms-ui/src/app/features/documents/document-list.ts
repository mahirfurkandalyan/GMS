import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { AuthService } from '../../core/auth.service';
import { DocumentService, GmsDocument, DOCUMENT_TYPES, DOCUMENT_STATUSES, DocPreviewKind, docTypeIcon } from '../../core/document.service';
import { ReleaseService, Release } from '../../core/release.service';
import { LanguageService } from '../../core/language.service';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPage } from '../../shared/ui/page/page';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsButton } from '../../shared/ui/button/button';
import { GmsField, GmsInput } from '../../shared/ui/field/field';
import { GmsModal } from '../../shared/ui/dialog/dialog';
import { GmsFormSection } from '../../shared/ui/form-section/form-section';
import { GmsUploadZone, UploadedFile } from '../../shared/ui/upload/upload';
import { GmsDataGrid, GmsCellDef, ColumnDef, RowActionEvent } from '../../shared/ui/data-grid/data-grid';
import { STANDARD_ROW_ACTIONS } from '../../shared/ui/data-grid/presets';
import { GmsState } from '../../shared/ui/state/state';
import { GmsContextSection } from '../../shared/ui/context-panel/context-section';
import { GmsRelationshipStrip, RelationNode } from '../../shared/ui/relationship-strip/relationship-strip';
import { GmsActivityFeed, ActivityItem } from '../../shared/ui/activity-feed/activity-feed';
import { GmsItemList, LinkItem } from '../../shared/ui/item-list/item-list';
import { ToastService } from '../../shared/ui/toast/toast';
import { ConfirmService } from '../../shared/ui/dialog/dialog';
import { STATUS_BADGES } from '../../shared/ui/badge/badge';
import { documentTypeLabel } from './document-vm';

@Component({
  selector: 'app-document-list',
  imports: [
    FormsModule, DatePipe, GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsField, GmsInput,
    GmsModal, GmsFormSection, GmsUploadZone, GmsDataGrid, GmsCellDef, GmsState,
    GmsContextSection, GmsRelationshipStrip, GmsActivityFeed, GmsItemList, TranslocoPipe
  ],
  providers: [provideTranslocoScope('documents')],
  templateUrl: './document-list.html',
  styleUrl: './document-list.scss'
})
export class DocumentList implements OnInit {
  private readonly documentService = inject(DocumentService);
  private readonly releaseService = inject(ReleaseService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  protected readonly documentTypes = DOCUMENT_TYPES;
  protected readonly statusOptions = DOCUMENT_STATUSES;
  protected readonly previewKinds = computed<{ key: DocPreviewKind; label: string }[]>(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { key: 'pdf', label: t.translate('documents.previewKind.pdf') },
      { key: 'sql', label: t.translate('documents.previewKind.sql') },
      { key: 'text', label: t.translate('documents.previewKind.text') },
      { key: 'image', label: t.translate('documents.previewKind.image') },
      { key: 'word', label: t.translate('documents.previewKind.word') },
      { key: 'excel', label: t.translate('documents.previewKind.excel') }
    ];
  });
  protected readonly statusLabel = (s: string) => {
    const meta = STATUS_BADGES[s];
    return meta?.labelKey ? this.transloco.translate(meta.labelKey) : s;
  };
  protected readonly typeLabel = (key: string) => documentTypeLabel(key, this.transloco);
  protected readonly typeIcon = docTypeIcon;

  protected readonly rows = signal<GmsDocument[]>([]);
  protected readonly loading = signal(false);
  protected readonly releases = signal<Release[]>([]);

  protected readonly fSearch = signal('');
  protected readonly fCategory = signal('');
  protected readonly fRelease = signal('');
  protected readonly fProject = signal('');
  protected readonly fType = signal('');
  protected readonly fOwner = signal('');
  protected readonly fDate = signal('');

  protected readonly releaseOptions = computed(() => [...new Set(this.rows().map((r) => r.releaseCode).filter((x): x is string => !!x))].sort());
  protected readonly projectOptions = computed(() => [...new Set(this.rows().map((r) => r.projectName))].sort());
  protected readonly ownerOptions = computed(() => [...new Set(this.rows().map((r) => r.owner))].sort());

  protected readonly filteredRows = computed(() => {
    const q = this.fSearch().trim().toLocaleLowerCase('tr');
    return this.rows().filter((d) => {
      const matchesText =
        !q ||
        d.code.toLocaleLowerCase('tr').includes(q) ||
        d.name.toLocaleLowerCase('tr').includes(q) ||
        d.owner.toLocaleLowerCase('tr').includes(q) ||
        (d.releaseCode ?? '').toLocaleLowerCase('tr').includes(q);
      return (
        matchesText &&
        (!this.fCategory() || d.category === this.fCategory()) &&
        (!this.fRelease() || d.releaseCode === this.fRelease()) &&
        (!this.fProject() || d.projectName === this.fProject()) &&
        (!this.fType() || d.preview === this.fType()) &&
        (!this.fOwner() || d.owner === this.fOwner()) &&
        (!this.fDate() || d.createdAt.slice(0, 10) >= this.fDate())
      );
    });
  });

  protected readonly hasFilters = computed(
    () => !!(this.fSearch() || this.fCategory() || this.fRelease() || this.fProject() || this.fType() || this.fOwner() || this.fDate())
  );

  protected readonly columns = computed<ColumnDef[]>(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { key: 'code', header: t.translate('documents.column.code'), sticky: true, sortable: true, width: '150px' },
      { key: 'name', header: t.translate('documents.column.name'), sortable: true },
      { key: 'category', header: t.translate('documents.column.category') },
      { key: 'releaseCode', header: t.translate('documents.column.release') },
      { key: 'changeCode', header: t.translate('documents.column.change') },
      { key: 'owner', header: t.translate('documents.column.owner') },
      { key: 'version', header: t.translate('documents.column.version') },
      { key: 'status', header: t.translate('documents.column.status'), type: 'badge', badgeKind: 'status', sortable: true },
      { key: 'createdAt', header: t.translate('documents.column.createdAt'), sortable: true },
      { key: 'updatedAt', header: t.translate('documents.column.updatedAt') }
    ];
  });

  protected readonly rowActions = STANDARD_ROW_ACTIONS;

  protected readonly relationChain = computed<RelationNode[]>(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { type: t.translate('documents.relation.domain'), label: t.translate('documents.relation.governance'), icon: 'shield', route: '/hub' },
      { type: t.translate('documents.relation.module'), label: t.translate('documents.breadcrumbCenter'), icon: 'document', current: true }
    ];
  });
  protected readonly panelActivity = computed<ActivityItem[]>(() => {
    this.language.current();
    const t = this.transloco;
    return [
      { actor: 'Mehmet Kaya', action: t.translate('documents.activity.uploadedValidationPlan'), time: t.translate('documents.activity.oneHourAgo'), icon: 'document' },
      { actor: 'Ayşe Yılmaz', action: t.translate('documents.activity.updatedSqlScript'), time: t.translate('documents.activity.threeHoursAgo'), icon: 'change' },
      { actor: 'Ali Vural', action: t.translate('documents.activity.archivedExecutionReport'), time: t.translate('documents.activity.yesterday'), icon: 'folder' }
    ];
  });
  protected readonly relatedReleases: LinkItem[] = [
    { id: 'r1', label: 'REL-2026-001', hint: 'EBR Migration · PROD', route: '/releases/07777777-7777-7777-7777-777777777701', icon: 'release' }
  ];

  // Upload form
  protected readonly uploadOpen = signal(false);
  protected readonly uploadError = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly formName = signal('');
  protected readonly formCategory = signal('validation-report');
  protected readonly formReleaseId = signal('');
  protected readonly formDescription = signal('');
  protected readonly formFiles = signal<UploadedFile[]>([]);
  protected readonly canUpload = computed(() => !!this.formName().trim() && !!this.formCategory() && this.formFiles().length > 0);

  ngOnInit(): void {
    this.load();
    this.releaseService.getReleases().subscribe({ next: (r) => this.releases.set(r), error: () => {} });
  }

  openDocument(row: GmsDocument): void {
    this.router.navigate(['/documents', row.id]);
  }

  resetFilters(): void {
    this.fSearch.set(''); this.fCategory.set(''); this.fRelease.set(''); this.fProject.set('');
    this.fType.set(''); this.fOwner.set(''); this.fDate.set('');
  }

  openUpload(): void {
    this.formName.set(''); this.formCategory.set('validation-report'); this.formReleaseId.set('');
    this.formDescription.set(''); this.formFiles.set([]); this.uploadError.set(null);
    this.uploadOpen.set(true);
  }

  onFiles(files: UploadedFile[]): void {
    this.formFiles.set(files);
    if (files.length && !this.formName().trim()) {
      this.formName.set(files[0].name.replace(/\.[^.]+$/, ''));
    }
  }

  onUpload(): void {
    this.uploadError.set(null);
    if (!this.canUpload()) {
      this.uploadError.set(this.transloco.translate('documents.form.validationError'));
      return;
    }
    const file = this.formFiles()[0];
    const release = this.releases().find((r) => r.id === this.formReleaseId());
    const owner = String(this.auth.getCurrentUser()?.fullName ?? this.transloco.translate('documents.unknownOwner'));
    const preview: DocPreviewKind = file.kind === 'other' ? 'text' : file.kind;
    this.submitting.set(true);
    this.documentService
      .upload({
        name: this.formName().trim(),
        category: this.formCategory(),
        fileName: file.name,
        preview,
        size: file.sizeLabel,
        releaseId: release?.id ?? null,
        releaseCode: release?.name ?? null,
        projectName: release?.projectName ?? '—',
        description: this.formDescription().trim(),
        owner
      })
      .subscribe({
        next: (created) => {
          this.submitting.set(false);
          this.uploadOpen.set(false);
          this.toast.success(this.transloco.translate('documents.toast.uploaded', { code: created.code }), this.transloco.translate('documents.toast.savedTitle'));
          this.load();
        },
        error: () => {
          this.submitting.set(false);
          this.uploadError.set(this.transloco.translate('documents.toast.uploadFailed'));
        }
      });
  }

  onRowAction(event: RowActionEvent): void {
    const d = event.row as GmsDocument;
    const t = this.transloco;
    switch (event.action) {
      case 'open': this.openDocument(d); break;
      case 'edit': this.toast.info(t.translate('documents.toast.editSoon', { code: d.code })); break;
      case 'duplicate': this.toast.success(t.translate('documents.toast.duplicated', { code: d.code }), t.translate('documents.toast.duplicatedTitle')); break;
      case 'copy-link':
        navigator.clipboard?.writeText(`${location.origin}/documents/${d.id}`);
        this.toast.success(t.translate('documents.toast.linkCopied'));
        break;
      case 'archive': this.toast.undo(t.translate('documents.toast.archived', { code: d.code }), () => this.toast.info(t.translate('documents.toast.restored'))); break;
      case 'delete':
        this.confirm.ask({
          title: t.translate('documents.toast.deleteTitle'),
          message: t.translate('documents.toast.deleteMessage', { code: d.code }),
          confirmText: t.translate('documents.toast.deleteConfirm'),
          variant: 'danger'
        }).then((ok) => { if (ok) this.toast.undo(t.translate('documents.toast.deleted', { code: d.code }), () => this.toast.info(t.translate('documents.toast.deleteRestored'))); });
        break;
    }
  }

  onExport(): void {
    this.toast.info(this.transloco.translate('documents.toast.exportSoon'));
  }

  reload(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.documentService.getDocuments().subscribe((d) => {
      this.rows.set(d);
      this.loading.set(false);
    });
  }
}
