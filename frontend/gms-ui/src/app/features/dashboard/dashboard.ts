import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { AuthService } from '../../core/auth.service';
import { CatalogService } from '../../core/catalog.service';
import { ReleaseService, Release } from '../../core/release.service';
import { TrainingService, Training } from '../../core/training.service';
import { NotificationService, NOTIFICATION_META } from '../../core/notification.service';
import { GmsIcon, IconName } from '../../shared/icon/icon';
import { GmsStat, StatTone } from '../../shared/ui/stat/stat';

interface Stat {
  label: string;
  value: string | number;
  icon: IconName;
  delta?: string;
  tone?: StatTone;
  spark?: number[];
}

interface SystemStatus {
  name: string;
  state: 'ok' | 'degraded' | 'down';
}

interface Activity {
  who: string;
  action: string;
  time: string;
  icon: IconName;
}

const RELEASE_STATUS: Record<string, { label: string; tone: string }> = {
  Draft: { label: 'Taslak', tone: 'neutral' },
  Planned: { label: 'Planlandı', tone: 'info' },
  InReview: { label: 'İncelemede', tone: 'warning' },
  Approved: { label: 'Onaylandı', tone: 'success' },
  Completed: { label: 'Tamamlandı', tone: 'success' },
  Cancelled: { label: 'İptal', tone: 'neutral' }
};

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, DatePipe, GmsIcon, GmsStat],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly catalog = inject(CatalogService);
  private readonly releaseService = inject(ReleaseService);
  private readonly trainingService = inject(TrainingService);
  private readonly notifService = inject(NotificationService);

  protected readonly notifMeta = NOTIFICATION_META;

  private readonly user = this.auth.currentUser;
  protected readonly firstName = computed(() => this.user()?.fullName?.split(' ')[0] ?? '');
  protected readonly primaryRole = computed(() => this.user()?.roles?.[0] ?? '');
  protected readonly greeting = signal('Merhaba');

  protected readonly loadingReleases = signal(true);
  private readonly releases = signal<Release[]>([]);
  private readonly projectCount = signal(0);

  protected readonly recentReleases = computed(() => this.releases().slice(0, 5));
  protected readonly upcomingReleases = computed(() =>
    this.releases()
      .filter((r) => r.plannedDate && r.status !== 'Completed' && r.status !== 'Cancelled')
      .sort((a, b) => (a.plannedDate! < b.plannedDate! ? -1 : 1))
      .slice(0, 4)
  );

  protected readonly myTasks = signal<Training[]>([]);
  protected readonly recentNotifications = computed(() => this.notifService.notifications().slice(0, 4));

  protected readonly stats = computed<Stat[]>(() => [
    { label: 'Planlanan Yayın', value: this.releases().filter((r) => r.status === 'Planned').length, icon: 'release', delta: '+2 bu ay', tone: 'up', spark: [1, 2, 1, 3, 2, 4, 3] },
    { label: 'Aktif Proje', value: String(this.projectCount()), icon: 'folder', delta: 'sabit', tone: 'neutral', spark: [2, 2, 2, 3, 2, 2, 2] },
    { label: 'Açık Change', value: '0', icon: 'change', delta: '—', tone: 'neutral', spark: [0, 1, 0, 0, 1, 0, 0] },
    { label: 'Bekleyen Onay', value: '0', icon: 'approval', delta: '—', tone: 'neutral', spark: [1, 0, 1, 0, 0, 0, 0] }
  ]);

  protected readonly systems: SystemStatus[] = [
    { name: 'API Servisi', state: 'ok' },
    { name: 'Veritabanı (SQL Server)', state: 'ok' },
    { name: 'Kimlik Doğrulama', state: 'ok' },
    { name: 'Arka Plan İşleri', state: 'degraded' }
  ];

  protected readonly activities: Activity[] = [
    { who: 'Ali Vural', action: 'yeni bir yayın oluşturdu', time: '10 dk önce', icon: 'release' },
    { who: 'Ayşe Yılmaz', action: 'bir onay verdi', time: '45 dk önce', icon: 'approval' },
    { who: 'Mehmet Kaya', action: 'bir doküman ekledi', time: '2 saat önce', icon: 'document' },
    { who: 'Zeynep Şahin', action: 'MES projesine katıldı', time: 'Dün', icon: 'team' }
  ];

  ngOnInit(): void {
    this.greeting.set(this.resolveGreeting());

    this.catalog.getProjects().subscribe({
      next: (projects) => this.projectCount.set(projects.length),
      error: () => this.projectCount.set(0)
    });

    this.releaseService.getReleases().subscribe({
      next: (releases) => {
        this.releases.set(releases);
        this.loadingReleases.set(false);
      },
      error: () => {
        this.releases.set([]);
        this.loadingReleases.set(false);
      }
    });

    this.trainingService.getTrainings().subscribe((t) =>
      this.myTasks.set(t.filter((x) => x.status === 'assigned').slice(0, 4))
    );
  }

  statusLabel(status: string): string {
    return RELEASE_STATUS[status]?.label ?? status;
  }

  statusTone(status: string): string {
    return 'badge--' + (RELEASE_STATUS[status]?.tone ?? 'neutral');
  }

  systemMeta(state: SystemStatus['state']): { label: string; dot: string } {
    switch (state) {
      case 'ok':
        return { label: 'Çalışıyor', dot: 'dot--green' };
      case 'degraded':
        return { label: 'Kısmi', dot: 'dot--amber' };
      default:
        return { label: 'Kesinti', dot: 'dot--red' };
    }
  }

  private resolveGreeting(): string {
    const hour = new Date().getHours();
    if (hour < 12) return 'Günaydın';
    if (hour < 18) return 'İyi günler';
    return 'İyi akşamlar';
  }
}
