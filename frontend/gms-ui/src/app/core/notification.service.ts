import { Injectable, computed, signal } from '@angular/core';

export type NotificationKind = 'release' | 'training' | 'mention' | 'project' | 'approval';
export type NotificationPriority = 'high' | 'normal' | 'low';

export interface NotificationAction {
  label: string;
  route: string;
}

export interface AppNotification {
  id: string;
  kind: NotificationKind;
  priority: NotificationPriority;
  title: string;
  detail: string;
  time: string;
  read: boolean;
  action?: NotificationAction;
}

export const NOTIFICATION_META: Record<NotificationKind, { label: string; badge: string }> = {
  release: { label: 'Yayın', badge: 'badge--info' },
  training: { label: 'Eğitim', badge: 'badge--warning' },
  mention: { label: 'Bahsetme', badge: 'badge--neutral' },
  project: { label: 'Proje', badge: 'badge--success' },
  approval: { label: 'Onay', badge: 'badge--info' }
};

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly items = signal<AppNotification[]>([
    { id: 'n-01', kind: 'approval', priority: 'high', title: 'Onayınız bekleniyor', detail: 'REL-2026-002 (MES Upgrade) onayınızı bekliyor.', time: '5 dk önce', read: false, action: { label: 'İncele', route: '/releases' } },
    { id: 'n-02', kind: 'release', priority: 'normal', title: 'Yayın onaylandı', detail: 'REL-2026-001 (EBR Migration) onaylandı.', time: '1 saat önce', read: false, action: { label: 'Yayına Git', route: '/releases' } },
    { id: 'n-03', kind: 'training', priority: 'high', title: 'Eğitim son tarihi yaklaşıyor', detail: 'GxP Temelleri eğitimi 15.08.2026 tarihinde sona eriyor.', time: '3 saat önce', read: false, action: { label: 'Eğitime Git', route: '/training' } },
    { id: 'n-04', kind: 'mention', priority: 'low', title: 'Bir yorumda bahsedildiniz', detail: 'Ayşe Yılmaz sizden MES yayınında bahsetti.', time: 'Dün', read: true },
    { id: 'n-05', kind: 'project', priority: 'normal', title: 'Yeni proje ataması', detail: 'EBR Migration projesine eklendiniz.', time: '2 gün önce', read: true, action: { label: 'Çalışanlar', route: '/employees' } }
  ]);

  readonly notifications = this.items.asReadonly();
  readonly unreadCount = computed(() => this.items().filter((n) => !n.read).length);

  markAllRead(): void {
    this.items.update((list) => list.map((n) => ({ ...n, read: true })));
  }

  markRead(id: string): void {
    this.items.update((list) => list.map((n) => (n.id === id ? { ...n, read: true } : n)));
  }
}
