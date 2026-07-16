import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AuthService } from '../../core/auth.service';
import { CatalogService } from '../../core/catalog.service';
import { ReleaseService } from '../../core/release.service';
import { TrainingService } from '../../core/training.service';
import { NotificationService, NOTIFICATION_META } from '../../core/notification.service';
import { RecentService } from '../../core/recent.service';
import { FavoritesService } from '../../core/favorites.service';
import { LanguageService } from '../../core/language.service';
import { GmsWorkspaceHeader, WorkspaceContext } from '../../shared/ui/workspace-header/workspace-header';
import { GmsWidget } from '../../shared/ui/widget/widget';
import { GmsItemList, LinkItem } from '../../shared/ui/item-list/item-list';
import { GmsActivityFeed, ActivityItem } from '../../shared/ui/activity-feed/activity-feed';
import { GmsNotificationList, NotificationRow } from '../../shared/ui/notification-list/notification-list';
import { GmsEmptyState } from '../../shared/ui/empty-state/empty-state';

interface SystemStatus {
  nameKey: string;
  state: 'ok' | 'degraded';
}

@Component({
  selector: 'app-hub',
  imports: [
    TranslocoPipe,
    GmsWorkspaceHeader,
    GmsWidget,
    GmsItemList,
    GmsActivityFeed,
    GmsNotificationList,
    GmsEmptyState
  ],
  templateUrl: './hub.html',
  styleUrl: './hub.scss'
})
export class Hub implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly catalog = inject(CatalogService);
  private readonly releaseService = inject(ReleaseService);
  private readonly trainingService = inject(TrainingService);
  private readonly notif = inject(NotificationService);
  private readonly recent = inject(RecentService);
  private readonly favorites = inject(FavoritesService);
  private readonly transloco = inject(TranslocoService);
  protected readonly language = inject(LanguageService);

  private readonly user = this.auth.currentUser;
  protected readonly firstName = computed(() => this.user()?.fullName?.split(' ')[0] ?? '');
  protected readonly greetingKey = signal('hub.greeting.default');
  protected readonly favorite = signal(false);

  // Workspace bağlamı
  protected readonly project = signal<WorkspaceContext | null>(null);
  protected readonly release = signal<WorkspaceContext | null>({ label: 'REL-2026-001' });

  protected readonly recentItems = this.recent.items;
  protected readonly favoriteItems = this.favorites.items;

  protected readonly loadingReleases = signal(true);
  protected readonly upcoming = signal<LinkItem[]>([]);
  protected readonly myTasks = signal<LinkItem[]>([]);

  protected readonly notifRows = computed<NotificationRow[]>(() =>
    this.notif.notifications().slice(0, 4).map((n) => ({
      id: n.id,
      categoryLabel: NOTIFICATION_META[n.kind].label,
      badgeClass: NOTIFICATION_META[n.kind].badge,
      priority: n.priority,
      title: n.title,
      detail: n.detail,
      time: n.time,
      read: n.read,
      actionLabel: n.action?.label,
      actionRoute: n.action?.route
    }))
  );

  protected readonly activity = computed<ActivityItem[]>(() => {
    this.language.current();
    const t = (key: string) => this.transloco.translate(key);
    return [
      { group: t('hub.activity.today'), actor: 'Ali Vural', action: t('hub.activity.createdRelease'), target: 'REL-2026-003', time: '10:24', icon: 'release' },
      { group: t('hub.activity.today'), actor: 'Ayşe Yılmaz', action: t('hub.activity.gaveApproval'), time: '09:50', icon: 'approval' },
      { group: t('hub.activity.yesterday'), actor: 'Mehmet Kaya', action: t('hub.activity.addedDocument'), time: '17:12', icon: 'document' },
      { group: t('hub.activity.yesterday'), actor: 'Zeynep Şahin', action: t('hub.activity.joinedProject'), time: '15:03', icon: 'team' }
    ];
  });

  protected readonly systems: SystemStatus[] = [
    { nameKey: 'hub.system.api', state: 'ok' },
    { nameKey: 'hub.system.database', state: 'ok' },
    { nameKey: 'hub.system.auth', state: 'ok' },
    { nameKey: 'hub.system.jobs', state: 'degraded' }
  ];

  ngOnInit(): void {
    this.greetingKey.set(this.resolveGreeting());

    this.catalog.getProjects().subscribe({
      next: (projects) => {
        if (projects.length) this.project.set({ label: projects[0].name, hint: projects[0].customerName });
      },
      error: () => this.project.set({ label: 'EBR Migration', hint: 'Abdi İbrahim' })
    });

    this.releaseService.getReleases().subscribe({
      next: (releases) => {
        this.upcoming.set(
          releases
            .filter((r) => r.plannedDate && r.status !== 'Completed' && r.status !== 'Cancelled')
            .slice(0, 4)
            .map((r) => ({
              id: r.id,
              label: r.name,
              hint: `${r.projectName} · ${r.environmentName}`,
              route: '/releases',
              icon: 'release'
            }))
        );
        this.loadingReleases.set(false);
      },
      error: () => this.loadingReleases.set(false)
    });

    this.trainingService.getTrainings().subscribe((trainings) => {
      this.myTasks.set(
        trainings
          .filter((t) => t.status === 'assigned')
          .slice(0, 4)
          .map((t) => ({
            id: t.id,
            label: t.title,
            hint: this.transloco.translate('hub.trainingHint', { progress: t.progress }),
            route: '/training',
            icon: 'training'
          }))
      );
    });
  }

  reloadReleases(): void {
    this.loadingReleases.set(true);
    this.releaseService.getReleases().subscribe({
      next: (releases) => {
        this.upcoming.set(
          releases
            .filter((r) => r.plannedDate && r.status !== 'Completed' && r.status !== 'Cancelled')
            .slice(0, 4)
            .map((r) => ({ id: r.id, label: r.name, hint: `${r.projectName} · ${r.environmentName}`, route: '/releases', icon: 'release' }))
        );
        this.loadingReleases.set(false);
      },
      error: () => this.loadingReleases.set(false)
    });
  }

  onNotifRead(id: string): void {
    this.notif.markRead(id);
  }

  removeFavorite(id: string): void {
    this.favorites.remove(id);
  }

  systemMeta(state: SystemStatus['state']): { labelKey: string; dot: string } {
    return state === 'ok'
      ? { labelKey: 'hub.system.ok', dot: 'dot--green' }
      : { labelKey: 'hub.system.degraded', dot: 'dot--amber' };
  }

  private resolveGreeting(): string {
    const hour = new Date().getHours();
    if (hour < 12) return 'hub.greeting.morning';
    if (hour < 18) return 'hub.greeting.afternoon';
    return 'hub.greeting.evening';
  }
}
