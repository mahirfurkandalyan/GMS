import { Injectable, computed, signal } from '@angular/core';
import { Observable, of } from 'rxjs';
import { IconName } from '../shared/icon/icon';
import { BadgeTone } from '../shared/ui/badge/badge';

/**
 * Notification Center data layer — the communication hub of GMS. Every module
 * (release, change, approval, validation, execution, workflow, document, audit)
 * eventually publishes notifications through this architecture. Frontend-only,
 * observable-based (`of()`) so a real notification/messaging API (SMTP, Teams,
 * SignalR, RabbitMQ …) can replace the service body without touching components.
 * No backend engine, no delivery here — only the reusable experience.
 */

export type NotifType = 'info' | 'success' | 'warning' | 'critical';
export type NotifStatus = 'unread' | 'read' | 'archived';
export type NotifModule =
  | 'release' | 'change' | 'approval' | 'validation' | 'execution' | 'workflow' | 'document' | 'audit' | 'system';

export interface TypeMeta { label: string; tone: BadgeTone; icon: IconName; }
export const TYPE_META: Record<NotifType, TypeMeta> = {
  info: { label: 'Bilgi', tone: 'info', icon: 'bell' },
  success: { label: 'Başarılı', tone: 'success', icon: 'check' },
  warning: { label: 'Uyarı', tone: 'warning', icon: 'bell' },
  critical: { label: 'Kritik', tone: 'danger', icon: 'shield' }
};
export function typeMeta(t: string): TypeMeta {
  return TYPE_META[t as NotifType] ?? { label: t, tone: 'neutral', icon: 'bell' };
}

export interface ModuleMeta { label: string; icon: IconName; }
export const MODULE_META: Record<NotifModule, ModuleMeta> = {
  release: { label: 'Yayın', icon: 'release' },
  change: { label: 'Değişiklik', icon: 'change' },
  approval: { label: 'Onay', icon: 'approval' },
  validation: { label: 'Doğrulama', icon: 'shield' },
  execution: { label: 'Yürütme', icon: 'execution' },
  workflow: { label: 'İş Akışı', icon: 'share' },
  document: { label: 'Doküman', icon: 'document' },
  audit: { label: 'Denetim', icon: 'audit' },
  system: { label: 'Sistem', icon: 'server' }
};
export function moduleMeta(m: string): ModuleMeta {
  return MODULE_META[m as NotifModule] ?? { label: m, icon: 'bell' };
}

export const NOTIF_STATUS_META: Record<NotifStatus, { label: string; tone: BadgeTone }> = {
  unread: { label: 'Okunmadı', tone: 'info' },
  read: { label: 'Okundu', tone: 'neutral' },
  archived: { label: 'Arşivlendi', tone: 'neutral' }
};

export interface RelatedObject { code: string; name: string; route: string; }

export interface GmsNotification {
  id: string;
  type: NotifType;
  priority: string; // Critical | High | Medium | Low (→ PRIORITY_BADGES)
  status: NotifStatus;
  module: NotifModule;
  title: string;
  description: string;
  related: RelatedObject | null;
  createdAt: string;
}

/* ── Notification Rules ─────────────────────────────────── */

export type NotifChannel = 'in-app' | 'email' | 'teams' | 'slack' | 'sms' | 'push';
export interface ChannelMeta { label: string; icon: IconName; placeholder: boolean; }
export const CHANNEL_META: Record<NotifChannel, ChannelMeta> = {
  'in-app': { label: 'Uygulama İçi', icon: 'bell', placeholder: false },
  email: { label: 'E-posta', icon: 'mail', placeholder: false },
  teams: { label: 'Microsoft Teams', icon: 'team', placeholder: true },
  slack: { label: 'Slack', icon: 'announcement', placeholder: true },
  sms: { label: 'SMS', icon: 'phone', placeholder: true },
  push: { label: 'Push Bildirimi', icon: 'bell', placeholder: true }
};
export function channelMeta(c: string): ChannelMeta {
  return CHANNEL_META[c as NotifChannel] ?? { label: c, icon: 'bell', placeholder: true };
}

export const TRIGGERS: { key: string; label: string; module: NotifModule }[] = [
  { key: 'approval-created', label: 'Onay Oluşturuldu', module: 'approval' },
  { key: 'approval-completed', label: 'Onay Tamamlandı', module: 'approval' },
  { key: 'release-planned', label: 'Yayın Planlandı', module: 'release' },
  { key: 'release-completed', label: 'Yayın Tamamlandı', module: 'release' },
  { key: 'validation-failed', label: 'Doğrulama Başarısız', module: 'validation' },
  { key: 'execution-started', label: 'Yürütme Başladı', module: 'execution' },
  { key: 'execution-failed', label: 'Yürütme Başarısız', module: 'execution' },
  { key: 'workflow-published', label: 'İş Akışı Yayınlandı', module: 'workflow' },
  { key: 'document-uploaded', label: 'Doküman Yüklendi', module: 'document' }
];
export function triggerLabel(key: string): string {
  return TRIGGERS.find((t) => t.key === key)?.label ?? key;
}

export interface NotificationRule {
  id: string;
  name: string;
  trigger: string; // trigger key
  channels: NotifChannel[];
  recipients: string[];
  priority: string;
  status: string; // Active | Inactive | Draft
  conditionsSummary: string;
  updatedAt: string;
}

/* ── Seed ───────────────────────────────────────────────── */

function n(
  id: string, type: NotifType, priority: string, status: NotifStatus, module: NotifModule,
  title: string, description: string, related: RelatedObject | null, createdAt: string
): GmsNotification {
  return { id, type, priority, status, module, title, description, related, createdAt };
}

const REL_001 = '07777777-7777-7777-7777-777777777701';

const SEED_NOTIFS: GmsNotification[] = [
  n('nt-01', 'critical', 'Critical', 'unread', 'execution', 'Yürütme başarısız oldu', 'EXE-2026-005 üretim yürütmesi bir adımda başarısız oldu; inceleme gerekiyor.', { code: 'EXE-2026-005', name: 'EBR v1.2 Yürütme', route: '/executions/exe-2026-005' }, '2026-07-07T08:40:00'),
  n('nt-02', 'warning', 'High', 'unread', 'approval', 'Onayınız bekleniyor', 'REL-2026-002 (MES Upgrade) onayınızı bekliyor.', { code: 'APR-2026-002', name: 'MES Upgrade Onayı', route: '/approvals' }, '2026-07-07T08:15:00'),
  n('nt-03', 'critical', 'Critical', 'unread', 'validation', 'Doğrulama başarısız', 'VAL-2026-004 doğrulamasında 2 yüksek önem dereceli bulgu tespit edildi.', { code: 'VAL-2026-004', name: 'MES Doğrulama', route: '/validation' }, '2026-07-07T07:50:00'),
  n('nt-04', 'info', 'Medium', 'unread', 'release', 'Bugün planlanan yayın', 'REL-2026-001 (EBR Migration) bugün üretime alınacak şekilde planlandı.', { code: 'REL-2026-001', name: 'EBR Migration v1', route: `/releases/${REL_001}` }, '2026-07-07T07:30:00'),
  n('nt-05', 'success', 'Medium', 'read', 'approval', 'Onay tamamlandı', 'APR-2026-004 onay süreci başarıyla tamamlandı.', { code: 'APR-2026-004', name: 'API Onayı', route: '/approvals' }, '2026-07-06T16:05:00'),
  n('nt-06', 'info', 'Low', 'read', 'workflow', 'İş akışı yayınlandı', 'Yayın İş Akışı v3 yayınlandı ve süreçlere bağlandı.', { code: 'WF-2026-001', name: 'Yayın İş Akışı', route: '/workflows/wf-release' }, '2026-07-06T14:00:00'),
  n('nt-07', 'success', 'Low', 'read', 'document', 'Doküman yüklendi', 'DOC-2026-002 (Şema Güncelleme Betiği) v3 yüklendi.', { code: 'DOC-2026-002', name: 'Şema Güncelleme Betiği', route: '/documents/doc-2026-002' }, '2026-07-05T17:10:00'),
  n('nt-08', 'warning', 'High', 'unread', 'change', 'Değişiklik reddedildi', 'CHG-2026-019 (Güvenlik yaması) ek test gerekçesiyle reddedildi.', { code: 'CHG-2026-019', name: 'Güvenlik Yaması', route: '/changes/chg-2026-019' }, '2026-07-04T13:22:00'),
  n('nt-09', 'info', 'Medium', 'read', 'execution', 'Yürütme başladı', 'EXE-2026-006 (Güvenlik Yaması) yürütmesi başlatıldı.', { code: 'EXE-2026-006', name: 'Güvenlik Yaması Yürütme', route: '/executions/exe-2026-006' }, '2026-07-06T08:47:00'),
  n('nt-10', 'info', 'Low', 'archived', 'system', 'Bakım tamamlandı', 'Planlı sistem bakımı başarıyla tamamlandı.', null, '2026-07-03T02:00:00'),
  n('nt-11', 'success', 'Medium', 'read', 'release', 'Yayın tamamlandı', 'REL-2026-006 (MES Upgrade) üretimde tamamlandı.', { code: 'REL-2026-006', name: 'MES Upgrade', route: '/releases' }, '2026-07-02T10:00:00'),
  n('nt-12', 'warning', 'Medium', 'unread', 'audit', 'Başarısız giriş denemesi', 'Yönetici hesabında başarısız giriş denemesi tespit edildi.', { code: 'AUD-2026-0005', name: 'Denetim Kaydı', route: '/audit/aud-0005' }, '2026-07-06T02:11:00')
];

const SEED_RULES: NotificationRule[] = [
  { id: 'rule-01', name: 'Onay Talebi Bildirimi', trigger: 'approval-created', channels: ['in-app', 'email'], recipients: ['Onaycı Rolü'], priority: 'High', status: 'Active', conditionsSummary: 'Tüm onay talepleri', updatedAt: '2026-06-28T10:00:00' },
  { id: 'rule-02', name: 'Onay Tamamlandı Bildirimi', trigger: 'approval-completed', channels: ['in-app'], recipients: ['Talep Eden'], priority: 'Medium', status: 'Active', conditionsSummary: 'Onay durumu = Tamamlandı', updatedAt: '2026-06-20T09:00:00' },
  { id: 'rule-03', name: 'Yayın Planlandı Bildirimi', trigger: 'release-planned', channels: ['in-app', 'email'], recipients: ['Proje Ekibi'], priority: 'Medium', status: 'Active', conditionsSummary: 'Ortam = PROD', updatedAt: '2026-06-15T14:00:00' },
  { id: 'rule-04', name: 'Yayın Tamamlandı Bildirimi', trigger: 'release-completed', channels: ['in-app'], recipients: ['Proje Ekibi', 'PMO'], priority: 'Low', status: 'Active', conditionsSummary: 'Tüm yayınlar', updatedAt: '2026-05-30T09:00:00' },
  { id: 'rule-05', name: 'Doğrulama Başarısız Uyarısı', trigger: 'validation-failed', channels: ['in-app', 'email', 'teams'], recipients: ['QA Ekibi', 'Mimar'], priority: 'Critical', status: 'Active', conditionsSummary: 'Bulgu önem = Yüksek', updatedAt: '2026-06-25T11:00:00' },
  { id: 'rule-06', name: 'Yürütme Başladı Bildirimi', trigger: 'execution-started', channels: ['in-app'], recipients: ['Yürütücü'], priority: 'Low', status: 'Inactive', conditionsSummary: 'Ortam = PROD', updatedAt: '2026-04-10T09:00:00' },
  { id: 'rule-07', name: 'Yürütme Başarısız Uyarısı', trigger: 'execution-failed', channels: ['in-app', 'email', 'sms'], recipients: ['Yürütücü', 'Yönetim'], priority: 'Critical', status: 'Active', conditionsSummary: 'Tüm başarısız yürütmeler', updatedAt: '2026-06-27T16:00:00' },
  { id: 'rule-08', name: 'İş Akışı Yayınlandı Bildirimi', trigger: 'workflow-published', channels: ['in-app'], recipients: ['Yöneticiler'], priority: 'Low', status: 'Draft', conditionsSummary: '—', updatedAt: '2026-06-10T09:00:00' },
  { id: 'rule-09', name: 'Doküman Yüklendi Bildirimi', trigger: 'document-uploaded', channels: ['in-app'], recipients: ['Doküman Sahibi'], priority: 'Low', status: 'Inactive', conditionsSummary: 'Kategori = Onay Kanıtı', updatedAt: '2026-05-05T09:00:00' }
];

@Injectable({ providedIn: 'root' })
export class NotificationCenterService {
  private readonly notifs = signal<GmsNotification[]>(SEED_NOTIFS);
  private readonly rules = signal<NotificationRule[]>(SEED_RULES);

  readonly all = this.notifs.asReadonly();
  readonly unreadCount = computed(() => this.notifs().filter((x) => x.status === 'unread').length);

  getNotifications(): Observable<GmsNotification[]> {
    return of(this.notifs());
  }
  markRead(id: string): void {
    this.notifs.update((list) => list.map((x) => (x.id === id ? { ...x, status: x.status === 'archived' ? x.status : 'read' } : x)));
  }
  markAllRead(): void {
    this.notifs.update((list) => list.map((x) => (x.status === 'unread' ? { ...x, status: 'read' } : x)));
  }
  archive(id: string): void {
    this.notifs.update((list) => list.map((x) => (x.id === id ? { ...x, status: 'archived' } : x)));
  }

  getRules(): Observable<NotificationRule[]> {
    return of(this.rules());
  }
  getRule(id: string): Observable<NotificationRule | undefined> {
    return of(this.rules().find((r) => r.id === id));
  }
}
