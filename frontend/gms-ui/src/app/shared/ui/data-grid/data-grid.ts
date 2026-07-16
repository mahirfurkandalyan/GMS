import {
  Component,
  ContentChildren,
  Directive,
  Input,
  QueryList,
  TemplateRef,
  computed,
  inject,
  input,
  output,
  signal
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { GmsIcon } from '../../icon/icon';
import { GmsBadge } from '../badge/badge';
import { GmsMenu, MenuItem } from '../menu/menu';
import { GmsSkeleton } from '../skeleton/skeleton';
import { GmsEmptyState } from '../empty-state/empty-state';
import { IconName } from '../../icon/icon';

export interface ColumnDef {
  key: string;
  header: string;
  sortable?: boolean;
  align?: 'left' | 'right' | 'center';
  width?: string;
  /** Freeze this column when horizontally scrolling. */
  sticky?: boolean;
  type?: 'text' | 'badge' | 'date';
  badgeKind?: 'status' | 'priority' | 'risk' | 'environment';
  hidden?: boolean;
}

export interface RowActionEvent {
  action: string;
  row: any;
}

/** Custom cell template: `<ng-template [gmsCell]="'name'" let-row>…</ng-template>` */
@Directive({ selector: '[gmsCell]', standalone: true })
export class GmsCellDef {
  @Input('gmsCell') key = '';
  readonly tpl = inject<TemplateRef<any>>(TemplateRef);
}

/**
 * GMS Data Grid — reusable enterprise table.
 * Features: sticky header + optional sticky first column, client sort, search,
 * bulk selection, pagination, row actions, column visibility, status badges,
 * skeleton + empty states, export event, keyboard row focus.
 */
@Component({
  selector: 'gms-data-grid',
  standalone: true,
  imports: [NgTemplateOutlet, GmsIcon, GmsBadge, GmsMenu, GmsSkeleton, GmsEmptyState, TranslocoPipe],
  templateUrl: './data-grid.html',
  styleUrl: './data-grid.scss'
})
export class GmsDataGrid {
  private readonly transloco = inject(TranslocoService);

  @ContentChildren(GmsCellDef) cellDefs?: QueryList<GmsCellDef>;

  readonly rows = input<any[]>([]);
  readonly columns = input<ColumnDef[]>([]);
  readonly idKey = input<string>('id');
  readonly loading = input(false);

  readonly searchable = input(true);
  readonly selectable = input(false);
  readonly pageSize = input(10);
  readonly rowActions = input<MenuItem[]>([]);
  readonly bulkActions = input<MenuItem[]>([]);
  readonly showToolbar = input(true);
  readonly emptyIcon = input<IconName>('inbox');
  readonly emptyTitle = input(this.transloco.translate('common.recordNotFound'));
  readonly emptyText = input(this.transloco.translate('common.noDataToDisplay'));

  readonly rowAction = output<RowActionEvent>();
  readonly bulkAction = output<{ action: string; rows: any[] }>();
  readonly selectionChange = output<any[]>();
  readonly exportRequested = output<void>();
  readonly rowClick = output<any>();

  protected readonly search = signal('');
  protected readonly sortKey = signal<string | null>(null);
  protected readonly sortDir = signal<'asc' | 'desc'>('asc');
  protected readonly page = signal(0);
  protected readonly selected = signal<Set<any>>(new Set());
  protected readonly hiddenCols = signal<Set<string>>(new Set());
  protected readonly colMenuOpen = signal(false);
  protected readonly ctxMenu = signal<{ x: number; y: number; row: any } | null>(null);

  protected readonly visibleColumns = computed(() =>
    this.columns().filter((c) => !c.hidden && !this.hiddenCols().has(c.key))
  );

  protected readonly colMenuItems = computed<MenuItem[]>(() =>
    this.columns().map((c) => ({
      label: (this.hiddenCols().has(c.key) ? '○ ' : '● ') + c.header,
      value: c.key
    }))
  );

  private readonly filtered = computed(() => {
    const q = this.search().trim().toLocaleLowerCase('tr');
    if (!q) return this.rows();
    const cols = this.visibleColumns();
    return this.rows().filter((r) =>
      cols.some((c) => String(r[c.key] ?? '').toLocaleLowerCase('tr').includes(q))
    );
  });

  private readonly sorted = computed(() => {
    const key = this.sortKey();
    if (!key) return this.filtered();
    const dir = this.sortDir() === 'asc' ? 1 : -1;
    return [...this.filtered()].sort((a, b) => {
      const av = a[key] ?? '';
      const bv = b[key] ?? '';
      return av < bv ? -dir : av > bv ? dir : 0;
    });
  });

  protected readonly total = computed(() => this.sorted().length);
  protected readonly pageCount = computed(() => {
    const size = this.pageSize();
    return size > 0 ? Math.max(1, Math.ceil(this.total() / size)) : 1;
  });

  protected readonly pagedRows = computed(() => {
    const size = this.pageSize();
    if (size <= 0) return this.sorted();
    const start = this.page() * size;
    return this.sorted().slice(start, start + size);
  });

  protected readonly allSelected = computed(() => {
    const rows = this.sorted();
    return rows.length > 0 && rows.every((r) => this.selected().has(r[this.idKey()]));
  });

  protected readonly selectedRows = computed(() => {
    const ids = this.selected();
    return this.rows().filter((r) => ids.has(r[this.idKey()]));
  });

  clearSelection(): void {
    this.selected.set(new Set());
    this.emitSelection();
  }

  onBulk(action: string): void {
    this.bulkAction.emit({ action, rows: this.selectedRows() });
  }

  cellTemplate(key: string): TemplateRef<any> | null {
    return this.cellDefs?.find((d) => d.key === key)?.tpl ?? null;
  }

  toggleSort(col: ColumnDef): void {
    if (!col.sortable) return;
    if (this.sortKey() === col.key) {
      this.sortDir.update((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortKey.set(col.key);
      this.sortDir.set('asc');
    }
  }

  onSearch(value: string): void {
    this.search.set(value);
    this.page.set(0);
  }

  toggleColumn(key: string): void {
    this.hiddenCols.update((set) => {
      const next = new Set(set);
      next.has(key) ? next.delete(key) : next.add(key);
      return next;
    });
  }

  isSelected(row: any): boolean {
    return this.selected().has(row[this.idKey()]);
  }

  toggleRow(row: any, event: Event): void {
    event.stopPropagation();
    const id = row[this.idKey()];
    this.selected.update((set) => {
      const next = new Set(set);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
    this.emitSelection();
  }

  toggleAll(): void {
    if (this.allSelected()) {
      this.selected.set(new Set());
    } else {
      this.selected.set(new Set(this.sorted().map((r) => r[this.idKey()])));
    }
    this.emitSelection();
  }

  private emitSelection(): void {
    const ids = this.selected();
    this.selectionChange.emit(this.rows().filter((r) => ids.has(r[this.idKey()])));
  }

  onRowKeydown(event: KeyboardEvent, row: any): void {
    const el = event.target as HTMLElement;
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      (el.nextElementSibling as HTMLElement)?.focus();
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      (el.previousElementSibling as HTMLElement)?.focus();
    } else if (event.key === 'Enter') {
      event.preventDefault();
      this.rowClick.emit(row);
    } else if (event.key === ' ' && this.selectable()) {
      event.preventDefault();
      this.toggleRow(row, event);
    }
  }

  onGridKeydown(event: KeyboardEvent): void {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'a' && this.selectable()) {
      const tag = (event.target as HTMLElement).tagName;
      if (tag === 'INPUT' || tag === 'TEXTAREA') return;
      event.preventDefault();
      this.toggleAll();
    }
  }

  openContext(event: MouseEvent, row: any): void {
    if (!this.rowActions().length) return;
    event.preventDefault();
    this.ctxMenu.set({ x: event.clientX, y: event.clientY, row });
  }

  onContextAction(action: string): void {
    const ctx = this.ctxMenu();
    if (ctx) {
      this.rowAction.emit({ action, row: ctx.row });
    }
    this.ctxMenu.set(null);
  }

  prevPage(): void { this.page.update((p) => Math.max(0, p - 1)); }
  nextPage(): void { this.page.update((p) => Math.min(this.pageCount() - 1, p + 1)); }

  formatCell(row: any, col: ColumnDef): string {
    const value = row[col.key];
    if (value == null || value === '') return '—';
    if (col.type === 'date') {
      const d = new Date(value);
      return isNaN(d.getTime()) ? String(value) : d.toLocaleDateString('tr-TR');
    }
    return String(value);
  }
}
