import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import { AuthService } from '../../core/auth.service';
import { WorkflowService, GmsWorkflow, WORKFLOW_CATEGORIES, WORKFLOW_STATUSES } from '../../core/workflow.service';
import { LanguageService } from '../../core/language.service';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPage } from '../../shared/ui/page/page';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsButton } from '../../shared/ui/button/button';
import { GmsMenu, MenuItem } from '../../shared/ui/menu/menu';
import { GmsFilterBar } from '../../shared/ui/filter-bar/filter-bar';
import { GmsField, GmsInput } from '../../shared/ui/field/field';
import { GmsModal, ConfirmService } from '../../shared/ui/dialog/dialog';
import { GmsFormSection } from '../../shared/ui/form-section/form-section';
import { GmsDataGrid, GmsCellDef, ColumnDef, RowActionEvent } from '../../shared/ui/data-grid/data-grid';
import { STANDARD_ROW_ACTIONS } from '../../shared/ui/data-grid/presets';
import { GmsState } from '../../shared/ui/state/state';
import { GmsContextSection } from '../../shared/ui/context-panel/context-section';
import { GmsActivityFeed, ActivityItem } from '../../shared/ui/activity-feed/activity-feed';
import { GmsItemList, LinkItem } from '../../shared/ui/item-list/item-list';
import { ToastService } from '../../shared/ui/toast/toast';
import { STATUS_BADGES } from '../../shared/ui/badge/badge';
import { categoryLabelKey } from './workflow-vm';

@Component({
  selector: 'app-workflow-list',
  imports: [
    FormsModule, DatePipe, GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsMenu, GmsFilterBar,
    GmsField, GmsInput, GmsModal, GmsFormSection, GmsDataGrid, GmsCellDef, GmsState,
    GmsContextSection, GmsActivityFeed, GmsItemList, TranslocoPipe
  ],
  providers: [provideTranslocoScope('workflows')],
  templateUrl: './workflow-list.html',
  styleUrl: './workflow-list.scss'
})
export class WorkflowList implements OnInit {
  private readonly workflowService = inject(WorkflowService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  protected readonly categories = WORKFLOW_CATEGORIES;
  protected readonly statuses = WORKFLOW_STATUSES;

  /** Category label, translated locally since WORKFLOW_CATEGORIES lives in a core service. */
  protected readonly categoryLabel = (key: string): string => this.transloco.translate(categoryLabelKey(key));

  /** Status label, resolved from the shared badge registry (covers Draft/Active/Inactive/Archived). */
  protected readonly statusLabel = (s: string): string => {
    const key = STATUS_BADGES[s]?.labelKey;
    return key ? this.transloco.translate(key) : s;
  };

  protected readonly rows = signal<GmsWorkflow[]>([]);
  protected readonly loading = signal(false);

  protected readonly fSearch = signal('');
  protected readonly fCategory = signal('');
  protected readonly fStatus = signal('');

  protected readonly filteredRows = computed(() => {
    const q = this.fSearch().trim().toLocaleLowerCase('tr');
    return this.rows().filter((w) =>
      (!q || w.name.toLocaleLowerCase('tr').includes(q) || w.code.toLocaleLowerCase('tr').includes(q) || w.createdBy.toLocaleLowerCase('tr').includes(q))
      && (!this.fCategory() || w.category === this.fCategory())
      && (!this.fStatus() || w.status === this.fStatus())
    );
  });

  protected readonly columns = computed<ColumnDef[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { key: 'name', header: t('workflows.list.col.name'), sticky: true, sortable: true, width: '220px' },
      { key: 'category', header: t('workflows.list.col.category') },
      { key: 'version', header: t('workflows.list.col.version') },
      { key: 'status', header: t('workflows.list.col.status'), type: 'badge', badgeKind: 'status', sortable: true },
      { key: 'createdBy', header: t('workflows.list.col.createdBy') },
      { key: 'updatedAt', header: t('workflows.list.col.updatedAt'), sortable: true },
      { key: 'usedBy', header: t('workflows.list.col.usedBy'), width: '110px' }
    ];
  });
  protected readonly rowActions = STANDARD_ROW_ACTIONS;

  protected readonly exportItems = computed<MenuItem[]>(() => {
    this.language.current();
    return [
      { label: this.transloco.translate('workflows.list.exportJson'), value: 'json', icon: 'document' },
      { label: this.transloco.translate('workflows.list.exportPdf'), value: 'pdf', icon: 'document' }
    ];
  });

  protected readonly panelActivity = computed<ActivityItem[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { actor: 'Furkan Demir', action: t('workflows.list.activity.item1'), time: t('workflows.list.time.twoHoursAgo'), icon: 'release' },
      { actor: 'Ayşe Yılmaz', action: t('workflows.list.activity.item2'), time: t('workflows.list.time.oneDayAgo'), icon: 'change' },
      { actor: 'Zeynep Şahin', action: t('workflows.list.activity.item3'), time: t('workflows.list.time.threeDaysAgo'), icon: 'approval' }
    ];
  });
  protected readonly activeWorkflows = computed<LinkItem[]>(() => {
    this.language.current();
    const active = this.transloco.translate('badge.status.Active');
    const usage = this.transloco.translate('workflows.list.usageSuffix');
    return [
      { id: 'wf-release', label: 'Yayın İş Akışı', hint: `${active} · 7 ${usage}`, route: '/workflows/wf-release', icon: 'release' },
      { id: 'wf-change', label: 'Değişiklik İş Akışı', hint: `${active} · 5 ${usage}`, route: '/workflows/wf-change', icon: 'change' }
    ];
  });

  // Create modal
  protected readonly createOpen = signal(false);
  protected readonly createError = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly formName = signal('');
  protected readonly formCategory = signal('release');
  protected readonly formDescription = signal('');
  protected readonly canCreate = computed(() => !!this.formName().trim() && !!this.formCategory());

  ngOnInit(): void {
    this.load();
  }

  openWorkflow(row: GmsWorkflow): void {
    this.router.navigate(['/workflows', row.id]);
  }

  onResetFilters(): void {
    this.fCategory.set(''); this.fStatus.set('');
  }

  openCreate(): void {
    this.formName.set(''); this.formCategory.set('release'); this.formDescription.set(''); this.createError.set(null);
    this.createOpen.set(true);
  }

  onCreate(): void {
    this.createError.set(null);
    if (!this.canCreate()) { this.createError.set(this.transloco.translate('workflows.list.createModal.errorRequired')); return; }
    const createdBy = String(this.auth.getCurrentUser()?.fullName ?? this.transloco.translate('workflows.list.unknownUser'));
    this.submitting.set(true);
    this.workflowService.create({ name: this.formName().trim(), category: this.formCategory(), description: this.formDescription().trim(), createdBy })
      .subscribe({
        next: (created) => {
          this.submitting.set(false);
          this.createOpen.set(false);
          this.toast.success(
            this.transloco.translate('workflows.list.toast.createdBody', { code: created.code }),
            this.transloco.translate('workflows.list.toast.createdTitle')
          );
          this.load();
          this.router.navigate(['/workflows', created.id]);
        },
        error: () => { this.submitting.set(false); this.createError.set(this.transloco.translate('workflows.list.createModal.errorCreateFailed')); }
      });
  }

  onRowAction(event: RowActionEvent): void {
    const w = event.row as GmsWorkflow;
    switch (event.action) {
      case 'open': this.openWorkflow(w); break;
      case 'edit': this.openWorkflow(w); break;
      case 'duplicate':
        this.toast.success(
          this.transloco.translate('workflows.list.toast.duplicated', { code: w.code }),
          this.transloco.translate('workflows.list.toast.duplicatedTitle')
        );
        break;
      case 'copy-link':
        navigator.clipboard?.writeText(`${location.origin}/workflows/${w.id}`);
        this.toast.success(this.transloco.translate('workflows.list.toast.linkCopied'));
        break;
      case 'archive':
        this.toast.undo(
          this.transloco.translate('workflows.list.toast.archived', { code: w.code }),
          () => this.toast.info(this.transloco.translate('workflows.list.toast.undone'))
        );
        break;
      case 'delete':
        this.confirm.ask({
          title: this.transloco.translate('workflows.list.toast.deleteConfirmTitle'),
          message: this.transloco.translate('workflows.list.toast.deleteConfirmMessage', { name: w.name }),
          confirmText: this.transloco.translate('workflows.list.delete'),
          variant: 'danger'
        }).then((ok) => {
          if (ok) {
            this.toast.undo(
              this.transloco.translate('workflows.list.toast.deleted', { code: w.code }),
              () => this.toast.info(this.transloco.translate('workflows.list.toast.deleteUndone'))
            );
          }
        });
        break;
    }
  }

  onExport(kind: string): void {
    this.toast.info(this.transloco.translate('workflows.list.toast.exportSoon', { kind: kind.toUpperCase() }));
  }
  onRefresh(): void {
    this.load();
    this.toast.success(this.transloco.translate('workflows.list.toast.refreshed'));
  }

  private load(): void {
    this.loading.set(true);
    this.workflowService.getWorkflows().subscribe((w) => {
      this.rows.set(w);
      this.loading.set(false);
    });
  }
}
