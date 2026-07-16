import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { TranslocoPipe, TranslocoService, provideTranslocoScope } from '@jsverse/transloco';
import {
  NotificationCenterService, NotificationRule, TRIGGERS,
  channelMeta, moduleMeta
} from '../../core/notification-center.service';
import { GmsIcon } from '../../shared/icon/icon';
import { GmsPage } from '../../shared/ui/page/page';
import { GmsPageHeader } from '../../shared/ui/page-header/page-header';
import { GmsButton } from '../../shared/ui/button/button';
import { GmsFilterBar } from '../../shared/ui/filter-bar/filter-bar';
import { GmsDataGrid, GmsCellDef, ColumnDef, RowActionEvent } from '../../shared/ui/data-grid/data-grid';
import { GmsModal } from '../../shared/ui/dialog/dialog';
import { GmsState } from '../../shared/ui/state/state';
import { GmsContextSection } from '../../shared/ui/context-panel/context-section';
import { GmsActivityFeed, ActivityItem } from '../../shared/ui/activity-feed/activity-feed';
import { GmsItemList, LinkItem } from '../../shared/ui/item-list/item-list';
import { GmsTimeline, TimelineItem } from '../../shared/ui/timeline/timeline';
import { MenuItem } from '../../shared/ui/menu/menu';
import { ToastService } from '../../shared/ui/toast/toast';
import { STATUS_BADGES, PRIORITY_BADGES } from '../../shared/ui/badge/badge';
import { LanguageService } from '../../core/language.service';

@Component({
  selector: 'app-notification-rules',
  imports: [
    FormsModule, DatePipe, GmsIcon, GmsPage, GmsPageHeader, GmsButton, GmsFilterBar,
    GmsDataGrid, GmsCellDef, GmsModal, GmsState, GmsContextSection, GmsActivityFeed, GmsItemList, GmsTimeline,
    TranslocoPipe
  ],
  providers: [provideTranslocoScope('notificationRules')],
  templateUrl: './notification-rules.html',
  styleUrl: './notification-rules.scss'
})
export class NotificationRules implements OnInit {
  private readonly center = inject(NotificationCenterService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  protected readonly channelMeta = channelMeta;
  protected readonly triggers = TRIGGERS;

  // Local translation lookups — the shared TRIGGERS/CHANNEL_META/MODULE_META/STATUS_BADGES
  // dictionaries in the core service and shared badge component hold hardcoded (or
  // key-based common-scope) labels; we resolve display text locally via our own
  // `notificationRules.*` scope instead of touching those files.
  protected triggerLabel(key: string): string {
    return this.transloco.translate('notificationRules.trigger.' + key);
  }
  protected channelLabel(c: string): string {
    return this.transloco.translate('notificationRules.channel.' + c);
  }
  protected moduleLabel(m: string): string {
    return this.transloco.translate('notificationRules.module.' + m);
  }
  protected statusLabel(s: string): string {
    return this.transloco.translate(STATUS_BADGES[s]?.labelKey ?? s);
  }
  protected priorityLabel(p: string): string {
    return this.transloco.translate(PRIORITY_BADGES[p]?.labelKey ?? p);
  }

  protected readonly rules = signal<NotificationRule[]>([]);
  protected readonly loading = signal(true);

  protected readonly fSearch = signal('');
  protected readonly fTrigger = signal('');
  protected readonly fStatus = signal('');

  protected readonly filtered = computed(() => {
    this.language.current();
    const q = this.fSearch().trim().toLocaleLowerCase('tr');
    return this.rules().filter((r) =>
      (!q || r.name.toLocaleLowerCase('tr').includes(q) || this.triggerLabel(r.trigger).toLocaleLowerCase('tr').includes(q))
      && (!this.fTrigger() || r.trigger === this.fTrigger())
      && (!this.fStatus() || r.status === this.fStatus())
    );
  });

  protected readonly columns = computed<ColumnDef[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { key: 'name', header: t('notificationRules.column.name'), sticky: true, sortable: true, width: '220px' },
      { key: 'trigger', header: t('notificationRules.column.trigger') },
      { key: 'channels', header: t('notificationRules.column.channels') },
      { key: 'status', header: t('notificationRules.column.status'), type: 'badge', badgeKind: 'status', sortable: true },
      { key: 'updatedAt', header: t('notificationRules.column.updatedAt'), sortable: true }
    ];
  });
  protected readonly rowActions = computed<MenuItem[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { label: t('notificationRules.action.view'), value: 'view', icon: 'search' },
      { label: t('notificationRules.action.edit'), value: 'edit', icon: 'change' },
      { label: t('notificationRules.action.duplicate'), value: 'duplicate', icon: 'grid' }
    ];
  });

  // Right panel
  protected readonly recentNotifs = computed<LinkItem[]>(() => {
    this.language.current();
    return [...this.center.all()].sort((a, b) => b.createdAt.localeCompare(a.createdAt)).slice(0, 5)
      .map((n): LinkItem => ({ id: n.id, label: n.title, hint: this.moduleLabel(n.module), route: '/notifications', icon: moduleMeta(n.module).icon }));
  });
  protected readonly recentRuleChanges = computed<LinkItem[]>(() => {
    this.language.current();
    return [...this.rules()].sort((a, b) => b.updatedAt.localeCompare(a.updatedAt)).slice(0, 4)
      .map((r): LinkItem => ({ id: r.id, label: r.name, hint: this.statusLabel(r.status), route: '/admin/notification-rules', icon: 'filter' }));
  });
  protected readonly upcomingEvents = computed<ActivityItem[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { actor: 'REL-2026-001', action: t('notificationRules.upcoming.releaseProdToday'), time: t('notificationRules.upcoming.today'), icon: 'release' },
      { actor: 'APR-2026-002', action: t('notificationRules.upcoming.approvalDueSoon'), time: t('notificationRules.upcoming.tomorrow'), icon: 'approval' },
      { actor: 'VAL-2026-004', action: t('notificationRules.upcoming.revalidationNeeded'), time: t('notificationRules.upcoming.in2Days'), icon: 'shield' }
    ];
  });

  // Lifecycle timeline (generic notification journey)
  protected readonly lifecycle = computed<TimelineItem[]>(() => {
    this.language.current();
    const t = (k: string) => this.transloco.translate(k);
    return [
      { title: t('notificationRules.lifecycle.created.title'), time: t('notificationRules.lifecycle.created.time'), tone: 'info', icon: 'plus', description: t('notificationRules.lifecycle.created.description') },
      { title: t('notificationRules.lifecycle.delivered.title'), time: t('notificationRules.lifecycle.delivered.time'), tone: 'info', icon: 'share', description: t('notificationRules.lifecycle.delivered.description') },
      { title: t('notificationRules.lifecycle.read.title'), time: t('notificationRules.lifecycle.read.time'), tone: 'success', icon: 'check', description: t('notificationRules.lifecycle.read.description') },
      { title: t('notificationRules.lifecycle.archived.title'), time: t('notificationRules.lifecycle.archived.time'), tone: 'neutral', icon: 'folder', description: t('notificationRules.lifecycle.archived.description') }
    ];
  });

  // Rule detail modal
  protected readonly detailOpen = signal(false);
  protected readonly selected = signal<NotificationRule | null>(null);

  ngOnInit(): void {
    this.center.getRules().subscribe((r) => { this.rules.set(r); this.loading.set(false); });
  }

  openDetail(rule: NotificationRule): void {
    this.selected.set(rule);
    this.detailOpen.set(true);
  }
  onRowAction(e: RowActionEvent): void {
    const r = e.row as NotificationRule;
    switch (e.action) {
      case 'view': this.openDetail(r); break;
      case 'duplicate':
        this.toast.success(
          this.transloco.translate('notificationRules.toast.duplicated', { name: r.name }),
          this.transloco.translate('notificationRules.toast.duplicatedTitle')
        );
        break;
      default: this.toast.info(this.transloco.translate('notificationRules.toast.editSoon'));
    }
  }
  onNewRule(): void {
    this.toast.info(this.transloco.translate('notificationRules.toast.newRuleSoon'));
  }
  onResetFilters(): void { this.fTrigger.set(''); this.fStatus.set(''); }
  back(): void { this.router.navigate(['/notifications']); }
}
