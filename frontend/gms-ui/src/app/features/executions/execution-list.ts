import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { ExecutionService, Execution, EXECUTION_STATUSES } from '../../core/execution.service';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPage } from '../../shared/ui/page/page';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsButton } from '../../shared/ui/button/button';
import { GmsDataGrid, GmsCellDef, ColumnDef, RowActionEvent } from '../../shared/ui/data-grid/data-grid';
import { MenuItem } from '../../shared/ui/menu/menu';
import { GmsState } from '../../shared/ui/state/state';
import { GmsContextSection } from '../../shared/ui/context-panel/context-section';
import { GmsRelationshipStrip, RelationNode } from '../../shared/ui/relationship-strip/relationship-strip';
import { GmsActivityFeed, ActivityItem } from '../../shared/ui/activity-feed/activity-feed';
import { GmsItemList, LinkItem } from '../../shared/ui/item-list/item-list';
import { ToastService } from '../../shared/ui/toast/toast';
import { STATUS_BADGES } from '../../shared/ui/badge/badge';

@Component({
  selector: 'app-execution-list',
  imports: [
    FormsModule, DatePipe, GmsIcon, GmsPage, GmsPageHeader, GmsButton,
    GmsDataGrid, GmsCellDef, GmsState, GmsContextSection, GmsRelationshipStrip, GmsActivityFeed, GmsItemList
  ],
  templateUrl: './execution-list.html',
  styleUrl: './execution-list.scss'
})
export class ExecutionList implements OnInit {
  private readonly executionService = inject(ExecutionService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly statusOptions = EXECUTION_STATUSES;
  protected readonly statusLabel = (s: string) => STATUS_BADGES[s]?.label ?? s;

  protected readonly rows = signal<Execution[]>([]);
  protected readonly loading = signal(false);

  protected readonly fSearch = signal('');
  protected readonly fRelease = signal('');
  protected readonly fEnv = signal('');
  protected readonly fStatus = signal('');
  protected readonly fExecutor = signal('');
  protected readonly fDate = signal('');

  protected readonly releaseOptions = computed(() => [...new Set(this.rows().map((r) => r.releaseCode))].sort());
  protected readonly envOptions = computed(() => [...new Set(this.rows().map((r) => r.environmentName))].sort());
  protected readonly executorOptions = computed(() => [...new Set(this.rows().map((r) => r.executor))].sort());

  protected readonly filteredRows = computed(() => {
    const q = this.fSearch().trim().toLocaleLowerCase('tr');
    return this.rows().filter((e) => {
      const matchesText =
        !q ||
        e.code.toLocaleLowerCase('tr').includes(q) ||
        e.releaseCode.toLocaleLowerCase('tr').includes(q) ||
        e.changeCode.toLocaleLowerCase('tr').includes(q) ||
        e.executor.toLocaleLowerCase('tr').includes(q);
      return (
        matchesText &&
        (!this.fRelease() || e.releaseCode === this.fRelease()) &&
        (!this.fEnv() || e.environmentName === this.fEnv()) &&
        (!this.fStatus() || e.status === this.fStatus()) &&
        (!this.fExecutor() || e.executor === this.fExecutor()) &&
        (!this.fDate() || e.createdAt.slice(0, 10) >= this.fDate())
      );
    });
  });

  protected readonly hasFilters = computed(
    () => !!(this.fSearch() || this.fRelease() || this.fEnv() || this.fStatus() || this.fExecutor() || this.fDate())
  );

  protected readonly runningCount = computed(() => this.rows().filter((e) => e.status === 'Running').length);

  protected readonly columns: ColumnDef[] = [
    { key: 'code', header: 'Yürütme No', sticky: true, sortable: true, width: '150px' },
    { key: 'releaseCode', header: 'Yayın' },
    { key: 'changeCode', header: 'İlgili Değişiklik' },
    { key: 'environmentName', header: 'Ortam', type: 'badge', badgeKind: 'environment' },
    { key: 'executor', header: 'Yürütücü' },
    { key: 'currentStep', header: 'Mevcut Adım' },
    { key: 'progress', header: 'İlerleme', width: '150px' },
    { key: 'status', header: 'Durum', type: 'badge', badgeKind: 'status', sortable: true },
    { key: 'startedAt', header: 'Başlangıç' },
    { key: 'completedAt', header: 'Bitiş' }
  ];

  protected readonly rowActions: MenuItem[] = [
    { label: 'Aç', value: 'open', icon: 'search' },
    { label: 'İlgili Değişiklik', value: 'change', icon: 'change' },
    { label: 'Bağlantıyı Kopyala', value: 'copy-link', icon: 'share' }
  ];

  protected readonly relationChain: RelationNode[] = [
    { type: 'Alan', label: 'Yönetişim', icon: 'shield', route: '/hub' },
    { type: 'Modül', label: 'Yürütme Merkezi', icon: 'execution', current: true }
  ];
  protected readonly panelActivity: ActivityItem[] = [
    { actor: 'Ali Vural', action: 'EXE-2026-001 başlattı', time: '1 saat önce', icon: 'execution' },
    { actor: 'Sistem', action: 'EXE-2026-003 tamamlandı', time: '2 saat önce', icon: 'check' },
    { actor: 'Mehmet Kaya', action: 'EXE-2026-005 başarısız', time: 'Dün', icon: 'close' }
  ];
  protected readonly relatedChanges: LinkItem[] = [
    { id: 'c1', label: 'CHG-2026-018', hint: 'Kullanıcı arayüzü güncellemesi', route: '/changes/chg-2026-018', icon: 'change' }
  ];

  ngOnInit(): void {
    this.load();
  }

  openExecution(row: Execution): void {
    this.router.navigate(['/executions', row.id]);
  }

  resetFilters(): void {
    this.fSearch.set(''); this.fRelease.set(''); this.fEnv.set(''); this.fStatus.set(''); this.fExecutor.set(''); this.fDate.set('');
  }

  onRowAction(event: RowActionEvent): void {
    const e = event.row as Execution;
    if (event.action === 'open') {
      this.openExecution(e);
    } else if (event.action === 'change') {
      this.router.navigate(['/changes', e.changeId]);
    } else if (event.action === 'copy-link') {
      navigator.clipboard?.writeText(`${location.origin}/executions/${e.id}`);
      this.toast.success('Bağlantı panoya kopyalandı.');
    }
  }

  onExport(): void {
    this.toast.info('Dışa aktarma yakında eklenecek.');
  }

  reload(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.executionService.getExecutions().subscribe((e) => {
      this.rows.set(e);
      this.loading.set(false);
    });
  }
}
