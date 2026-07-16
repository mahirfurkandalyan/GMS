import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import {
  NotificationCenterService, GmsNotification, NotifModule,
  typeMeta, moduleMeta, NOTIF_STATUS_META, MODULE_META
} from '../../core/notification-center.service';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPage } from '../../shared/ui/page/page';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsButton } from '../../shared/ui/button/button';
import { GmsFilterBar } from '../../shared/ui/filter-bar/filter-bar';
import { GmsSectionNav, SectionNavGroup } from '../../shared/ui/section-nav/section-nav';
import { GmsDataGrid, GmsCellDef, ColumnDef, RowActionEvent } from '../../shared/ui/data-grid/data-grid';
import { GmsContextSection } from '../../shared/ui/context-panel/context-section';
import { MenuItem } from '../../shared/ui/menu/menu';
import { ToastService } from '../../shared/ui/toast/toast';
import { PRIORITY_BADGES } from '../../shared/ui/badge/badge';
import { LanguageService } from '../../core/language.service';

@Component({
  selector: 'app-notifications',
  imports: [
    FormsModule, DatePipe, GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsFilterBar,
    GmsSectionNav, GmsDataGrid, GmsCellDef, GmsContextSection, TranslocoPipe
  ],
  providers: [provideTranslocoScope('notifications')],
  templateUrl: './notifications.html',
  styleUrl: './notifications.scss'
})
export class Notifications implements OnInit {
  private readonly center = inject(NotificationCenterService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  protected readonly typeMeta = typeMeta;
  protected readonly moduleMeta = moduleMeta;
  protected readonly statusMeta = (s: string) => NOTIF_STATUS_META[s as keyof typeof NOTIF_STATUS_META] ?? { label: s, tone: 'neutral' as const };

  // Local translation lookups — the shared NOTIFICATION_META/MODULE_META/NOTIF_STATUS_META
  // dictionaries in the core service hold hardcoded Turkish labels; we translate locally
  // via our own `notifications.*` scope instead of touching the core service.
  protected typeLabel(t: string): string {
    return this.transloco.translate('notifications.type.' + t);
  }
  protected moduleLabel(m: string): string {
    return this.transloco.translate('notifications.module.' + m);
  }
  protected statusLabel(s: string): string {
    return this.transloco.translate('notifications.status.' + s);
  }
  protected priorityLabel(p: string): string {
    return this.transloco.translate(PRIORITY_BADGES[p]?.labelKey ?? p);
  }

  protected readonly notifs = this.center.all;
  protected readonly unreadCount = this.center.unreadCount;

  // Left menu views
  private readonly moduleKeys: NotifModule[] = ['release', 'change', 'approval', 'validation', 'execution', 'workflow', 'system'];
  protected readonly activeView = signal('all');

  private count(pred: (n: GmsNotification) => boolean): number {
    return this.notifs().filter(pred).length;
  }
  protected readonly navGroups = computed<SectionNavGroup[]>(() => {
    this.language.current();
    const c = (n: number) => (n ? String(n) : undefined);
    return [
      {
        title: null,
        items: [
          { key: 'all', label: this.transloco.translate('notifications.nav.all'), icon: 'inbox', badge: c(this.count((n) => n.status !== 'archived')) },
          { key: 'unread', label: this.transloco.translate('notifications.nav.unread'), icon: 'bell', badge: c(this.count((n) => n.status === 'unread')) },
          { key: 'read', label: this.transloco.translate('notifications.nav.read'), icon: 'check', badge: c(this.count((n) => n.status === 'read')) },
          { key: 'critical', label: this.transloco.translate('notifications.nav.critical'), icon: 'shield', badge: c(this.count((n) => n.type === 'critical' && n.status !== 'archived')) }
        ]
      },
      {
        title: this.transloco.translate('notifications.nav.modulesTitle'),
        items: this.moduleKeys.map((m) => ({
          key: m, label: this.moduleLabel(m), icon: MODULE_META[m].icon,
          badge: c(this.count((n) => n.module === m && n.status !== 'archived'))
        }))
      }
    ];
  });

  // Toolbar filters
  protected readonly fSearch = signal('');
  protected readonly fType = signal('');
  protected readonly fPriority = signal('');
  protected readonly fStatus = signal('');
  protected readonly fModule = signal('');
  protected readonly fDate = signal('');

  protected readonly typeOptions: { key: string; labelKey: string }[] = [
    { key: 'info', labelKey: 'notifications.type.info' },
    { key: 'success', labelKey: 'notifications.type.success' },
    { key: 'warning', labelKey: 'notifications.type.warning' },
    { key: 'critical', labelKey: 'notifications.type.critical' }
  ];
  protected readonly priorityOptions = ['Critical', 'High', 'Medium', 'Low'];
  protected readonly statusOptions: { key: string; labelKey: string }[] = [
    { key: 'unread', labelKey: 'notifications.status.unread' },
    { key: 'read', labelKey: 'notifications.status.read' },
    { key: 'archived', labelKey: 'notifications.status.archived' }
  ];
  protected readonly moduleOptions = this.moduleKeys;

  private matchesView(n: GmsNotification): boolean {
    const v = this.activeView();
    if (v === 'unread') return n.status === 'unread';
    if (v === 'read') return n.status === 'read';
    if (v === 'critical') return n.type === 'critical' && n.status !== 'archived';
    if (this.moduleKeys.includes(v as NotifModule)) return n.module === v && n.status !== 'archived';
    return n.status !== 'archived'; // 'all'
  }

  protected readonly filtered = computed(() => {
    const q = this.fSearch().trim().toLocaleLowerCase('tr');
    return this.notifs().filter((n) => {
      if (!this.matchesView(n)) return false;
      // Archived hidden unless explicitly requested via the status filter.
      if (n.status === 'archived' && this.fStatus() !== 'archived') return false;
      const text = !q || n.title.toLocaleLowerCase('tr').includes(q)
        || n.description.toLocaleLowerCase('tr').includes(q)
        || (n.related?.code ?? '').toLocaleLowerCase('tr').includes(q);
      return text
        && (!this.fType() || n.type === this.fType())
        && (!this.fPriority() || n.priority === this.fPriority())
        && (!this.fStatus() || n.status === this.fStatus())
        && (!this.fModule() || n.module === this.fModule())
        && (!this.fDate() || n.createdAt.slice(0, 10) >= this.fDate());
    }).sort((a, b) => b.createdAt.localeCompare(a.createdAt));
  });

  protected readonly columns = computed<ColumnDef[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { key: 'priority', header: t('notifications.column.priority'), type: 'badge', badgeKind: 'priority', sortable: true, width: '110px' },
      { key: 'title', header: t('notifications.column.title'), sticky: true, sortable: true, width: '280px' },
      { key: 'module', header: t('notifications.column.module') },
      { key: 'related', header: t('notifications.column.related') },
      { key: 'createdAt', header: t('notifications.column.createdAt'), sortable: true },
      { key: 'status', header: t('notifications.column.status') }
    ];
  });
  protected readonly rowActions = computed<MenuItem[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { label: t('notifications.action.openRelated'), value: 'open', icon: 'chevron-right' },
      { label: t('notifications.action.markRead'), value: 'read', icon: 'check' },
      { label: t('notifications.action.archive'), value: 'archive', icon: 'folder' }
    ];
  });

  // Selection → detail panel
  protected readonly selectedId = signal<string | null>(null);
  protected readonly selected = computed(() => this.notifs().find((n) => n.id === this.selectedId()) ?? null);

  ngOnInit(): void {
    // Preselect the first notification so the detail panel is populated.
    this.selectedId.set(this.filtered()[0]?.id ?? null);
  }

  select(row: GmsNotification): void {
    this.selectedId.set(row.id);
    if (row.status === 'unread') this.center.markRead(row.id);
  }

  onView(key: string): void {
    this.activeView.set(key);
    this.selectedId.set(this.filtered()[0]?.id ?? null);
  }

  onRowAction(e: RowActionEvent): void {
    const nx = e.row as GmsNotification;
    switch (e.action) {
      case 'open': this.openRelated(nx); break;
      case 'read':
        this.center.markRead(nx.id);
        this.toast.success(this.transloco.translate('notifications.toast.markedRead'));
        break;
      case 'archive':
        this.center.archive(nx.id);
        this.toast.undo(
          this.transloco.translate('notifications.toast.archived', { name: nx.title }),
          () => this.toast.info(this.transloco.translate('notifications.toast.undone'))
        );
        break;
    }
  }

  openRelated(nx: GmsNotification | null): void {
    if (nx?.related) this.router.navigateByUrl(nx.related.route);
    else this.toast.info(this.transloco.translate('notifications.toast.noRelated'));
  }
  markSelectedRead(): void {
    const s = this.selected();
    if (s) {
      this.center.markRead(s.id);
      this.toast.success(this.transloco.translate('notifications.toast.markedRead'));
    }
  }
  archiveSelected(): void {
    const s = this.selected();
    if (s) {
      this.center.archive(s.id);
      this.toast.undo(
        this.transloco.translate('notifications.toast.archived', { name: s.title }),
        () => this.toast.info(this.transloco.translate('notifications.toast.undone'))
      );
    }
  }

  markAllRead(): void {
    this.center.markAllRead();
    this.toast.success(this.transloco.translate('notifications.toast.allMarkedRead'));
  }
  onRefresh(): void {
    this.toast.success(this.transloco.translate('notifications.toast.refreshed'));
  }
  onResetFilters(): void {
    this.fType.set(''); this.fPriority.set(''); this.fStatus.set(''); this.fModule.set(''); this.fDate.set('');
  }
  goRules(): void {
    this.router.navigate(['/admin/notification-rules']);
  }
}
