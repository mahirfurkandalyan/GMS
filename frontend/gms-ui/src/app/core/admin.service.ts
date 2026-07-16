import { Injectable, signal } from '@angular/core';
import { Observable, of } from 'rxjs';
import { IconName } from '../shared/icon/icon';

/**
 * Administration Center data layer — the Core Configuration Center of GMS.
 * Holds every master definition (customers, projects, environments, users,
 * roles) that the rest of the platform will eventually consume.
 *
 * Frontend-only, observable-based (`of()`) so a real config API can replace the
 * service body without touching components. No auth / permission / integration
 * engine is implemented here — only the reusable master-data shape.
 */

export type HealthLevel = 'healthy' | 'degraded' | 'down';

/** General section — platform + infrastructure health snapshot. */
export interface SystemInfo {
  platformVersion: string;
  databaseStatus: HealthLevel;
  apiStatus: HealthLevel;
  lastBackup: string; // placeholder
  environment: string;
  appHealth: number; // 0..100
  license: string; // placeholder
  uptime: string;
}

export interface AdminCustomer {
  id: string;
  code: string;
  name: string;
  status: string; // Active | Inactive
  projectCount: number;
  createdAt: string;
}

export interface AdminProject {
  id: string;
  code: string;
  name: string;
  customerName: string;
  owner: string;
  status: string;
  environmentCount: number;
}

export interface AdminEnvironment {
  id: string;
  name: string;
  projectName: string;
  type: string; // DEV | TEST | UAT | PREPROD | PROD
  status: string;
  lastDeployment: string;
}

export interface AdminUser {
  id: string;
  fullName: string;
  email: string;
  role: string;
  department: string;
  status: string; // Active | Inactive
  lastLogin: string;
}

export interface AdminRole {
  id: string;
  name: string;
  description: string;
  userCount: number;
  permissions: number;
  status: string;
}

/** Master option lists — reused by every future module's forms/filters. */
export const ENVIRONMENT_TYPES = ['DEV', 'TEST', 'UAT', 'PREPROD', 'PROD'];
export const CONFIG_STATUSES = ['Active', 'Inactive'];
export const SYSTEM_ROLES = ['Administrator', 'Architect', 'Requester', 'QA Specialist', 'Executor', 'Viewer'];

/** A configuration section inside Administration's secondary navigation. */
export interface AdminSection {
  key: string;
  label: string;
  icon: IconName;
  implemented: boolean;
  hint: string;
}

/** Grouped secondary navigation — data-driven so new sections are one entry. */
export interface AdminNavGroup {
  title: string | null;
  sections: AdminSection[];
}

export const ADMIN_NAV: AdminNavGroup[] = [
  {
    title: null,
    sections: [{ key: 'general', label: 'Genel', icon: 'dashboard', implemented: true, hint: 'Platform ve altyapı durumu' }]
  },
  {
    title: 'Kuruluş',
    sections: [
      { key: 'customers', label: 'Müşteriler', icon: 'briefcase', implemented: true, hint: 'Müşteri tanımları' },
      { key: 'projects', label: 'Projeler', icon: 'folder', implemented: true, hint: 'Proje tanımları' },
      { key: 'environments', label: 'Ortamlar', icon: 'server', implemented: true, hint: 'Ortam tanımları' }
    ]
  },
  {
    title: 'Erişim Yönetimi',
    sections: [
      { key: 'users', label: 'Kullanıcılar', icon: 'employees', implemented: true, hint: 'Kullanıcı hesapları' },
      { key: 'roles', label: 'Roller', icon: 'lock', implemented: true, hint: 'Rol ve yetki tanımları' },
      { key: 'teams', label: 'Takımlar', icon: 'team', implemented: false, hint: 'Takım yapılandırması' }
    ]
  },
  {
    title: 'Sistem',
    sections: [
      { key: 'templates', label: 'Şablonlar', icon: 'document', implemented: false, hint: 'İş akışı ve doküman şablonları' },
      { key: 'notifications', label: 'Bildirimler', icon: 'bell', implemented: false, hint: 'Bildirim kuralları ve kanallar' },
      { key: 'integrations', label: 'Entegrasyonlar', icon: 'share', implemented: false, hint: 'Dış sistem bağlantıları' },
      { key: 'system-settings', label: 'Sistem Ayarları', icon: 'filter', implemented: false, hint: 'Genel platform ayarları' }
    ]
  }
];

/** Future capability catalog per placeholder section — prepares architecture only. */
export interface FutureFeature {
  name: string;
  description: string;
  icon: IconName;
}

export const FUTURE_FEATURES: Record<string, FutureFeature[]> = {
  teams: [
    { name: 'Takım Yapılandırması', description: 'Takımları projelere ve rollere bağlayın.', icon: 'team' },
    { name: 'Sorumluluk Matrisi', description: 'RACI tabanlı görev dağılımı.', icon: 'orgchart' }
  ],
  templates: [
    { name: 'İş Akışı Şablonları', description: 'Onay ve yürütme akışları için yeniden kullanılabilir şablonlar.', icon: 'change' },
    { name: 'Doküman Şablonları', description: 'Standart doküman formatları.', icon: 'document' }
  ],
  notifications: [
    { name: 'Bildirim Kuralları', description: 'Olay tabanlı bildirim tetikleyicileri.', icon: 'bell' },
    { name: 'SMTP', description: 'E-posta sunucusu yapılandırması.', icon: 'mail' },
    { name: 'Microsoft Teams', description: 'Teams kanal bildirimleri.', icon: 'team' },
    { name: 'Slack', description: 'Slack webhook entegrasyonu.', icon: 'announcement' }
  ],
  integrations: [
    { name: 'Azure DevOps', description: 'Boards ve Pipelines senkronizasyonu.', icon: 'hub' },
    { name: 'Jira', description: 'Issue ve sprint entegrasyonu.', icon: 'inbox' },
    { name: 'GitHub', description: 'Depo ve PR bağlantıları.', icon: 'share' },
    { name: 'LDAP', description: 'Dizin tabanlı kullanıcı senkronizasyonu.', icon: 'employees' },
    { name: 'SSO', description: 'Tek oturum açma (SAML / OIDC).', icon: 'lock' },
    { name: 'API Anahtarları', description: 'Programatik erişim anahtarları.', icon: 'server' }
  ],
  'system-settings': [
    { name: 'Denetim Yapılandırması', description: 'Denetim kaydı kapsamı ve saklama.', icon: 'audit' },
    { name: 'Bölgesel Ayarlar', description: 'Dil, saat dilimi ve biçimler.', icon: 'calendar' },
    { name: 'Güvenlik Politikaları', description: 'Parola ve oturum politikaları.', icon: 'shield' }
  ]
};

const CUSTOMERS: AdminCustomer[] = [
  { id: 'c-abdi', code: 'CUST-001', name: 'Abdi İbrahim', status: 'Active', projectCount: 1, createdAt: '2025-08-01T09:00:00' },
  { id: 'c-bilim', code: 'CUST-002', name: 'Bilim İlaç', status: 'Active', projectCount: 1, createdAt: '2025-09-12T09:00:00' },
  { id: 'c-nobel', code: 'CUST-003', name: 'Nobel İlaç', status: 'Active', projectCount: 0, createdAt: '2025-11-03T09:00:00' },
  { id: 'c-deva', code: 'CUST-004', name: 'Deva Holding', status: 'Inactive', projectCount: 0, createdAt: '2025-06-20T09:00:00' }
];

const PROJECTS: AdminProject[] = [
  { id: 'p-ebr', code: 'EBR-MIG', name: 'EBR Migration', customerName: 'Abdi İbrahim', owner: 'Furkan Demir', status: 'Active', environmentCount: 4 },
  { id: 'p-mes', code: 'MES-UPG', name: 'MES Upgrade', customerName: 'Bilim İlaç', owner: 'Ayşe Yılmaz', status: 'Active', environmentCount: 4 },
  { id: 'p-lims', code: 'LIMS-INT', name: 'LIMS Entegrasyonu', customerName: 'Nobel İlaç', owner: 'Zeynep Şahin', status: 'Inactive', environmentCount: 1 }
];

const ENVIRONMENTS: AdminEnvironment[] = [
  { id: 'env-ebr-dev', name: 'EBR-DEV', projectName: 'EBR Migration', type: 'DEV', status: 'Active', lastDeployment: '2026-06-28T14:00:00' },
  { id: 'env-ebr-test', name: 'EBR-TEST', projectName: 'EBR Migration', type: 'TEST', status: 'Active', lastDeployment: '2026-06-30T10:00:00' },
  { id: 'env-ebr-uat', name: 'EBR-UAT', projectName: 'EBR Migration', type: 'UAT', status: 'Active', lastDeployment: '2026-07-02T09:00:00' },
  { id: 'env-ebr-prod', name: 'EBR-PROD', projectName: 'EBR Migration', type: 'PROD', status: 'Active', lastDeployment: '2026-07-05T08:00:00' },
  { id: 'env-mes-dev', name: 'MES-DEV', projectName: 'MES Upgrade', type: 'DEV', status: 'Active', lastDeployment: '2026-06-25T16:00:00' },
  { id: 'env-mes-preprod', name: 'MES-PREPROD', projectName: 'MES Upgrade', type: 'PREPROD', status: 'Active', lastDeployment: '2026-07-01T11:00:00' },
  { id: 'env-mes-prod', name: 'MES-PROD', projectName: 'MES Upgrade', type: 'PROD', status: 'Active', lastDeployment: '2026-07-04T09:00:00' },
  { id: 'env-lims-dev', name: 'LIMS-DEV', projectName: 'LIMS Entegrasyonu', type: 'DEV', status: 'Inactive', lastDeployment: '2026-03-10T09:00:00' }
];

const USERS: AdminUser[] = [
  { id: 'emp-01', fullName: 'Furkan Demir', email: 'furkan.demir@gms.local', role: 'Architect', department: 'Mühendislik', status: 'Active', lastLogin: '2026-07-06T08:30:00' },
  { id: 'emp-02', fullName: 'Ayşe Yılmaz', email: 'ayse.yilmaz@gms.local', role: 'Requester', department: 'Mühendislik', status: 'Active', lastLogin: '2026-07-05T17:10:00' },
  { id: 'emp-03', fullName: 'Mehmet Kaya', email: 'mehmet.kaya@gms.local', role: 'QA Specialist', department: 'Kalite', status: 'Active', lastLogin: '2026-07-06T09:05:00' },
  { id: 'emp-04', fullName: 'Zeynep Şahin', email: 'zeynep.sahin@gms.local', role: 'Requester', department: 'PMO', status: 'Inactive', lastLogin: '2026-06-20T14:00:00' },
  { id: 'emp-05', fullName: 'Ali Vural', email: 'ali.vural@gms.local', role: 'Executor', department: 'Altyapı', status: 'Active', lastLogin: '2026-07-05T22:40:00' },
  { id: 'emp-06', fullName: 'Elif Aydın', email: 'elif.aydin@gms.local', role: 'Viewer', department: 'PMO', status: 'Active', lastLogin: '2026-07-04T11:20:00' },
  { id: 'emp-sa', fullName: 'System Administrator', email: 'admin@centra.com.tr', role: 'Administrator', department: 'Altyapı', status: 'Active', lastLogin: '2026-07-06T09:30:00' }
];

const ROLES: AdminRole[] = [
  { id: 'role-admin', name: 'Administrator', description: 'Tüm modüllere ve yapılandırmalara tam erişim.', userCount: 1, permissions: 48, status: 'Active' },
  { id: 'role-arch', name: 'Architect', description: 'Yayın ve değişiklik tasarımı, mimari kararlar.', userCount: 1, permissions: 32, status: 'Active' },
  { id: 'role-req', name: 'Requester', description: 'Yayın ve değişiklik talebi oluşturma.', userCount: 2, permissions: 18, status: 'Active' },
  { id: 'role-qa', name: 'QA Specialist', description: 'Doğrulama ve test yürütme.', userCount: 1, permissions: 22, status: 'Active' },
  { id: 'role-exec', name: 'Executor', description: 'Onaylı yayınları üretimde yürütme.', userCount: 1, permissions: 20, status: 'Active' },
  { id: 'role-view', name: 'Viewer', description: 'Salt okunur görüntüleme erişimi.', userCount: 1, permissions: 8, status: 'Active' }
];

const SYSTEM_INFO: SystemInfo = {
  platformVersion: 'GMS 2.6.0',
  databaseStatus: 'healthy',
  apiStatus: 'healthy',
  lastBackup: '6 Tem 2026, 03:00',
  environment: 'Production',
  appHealth: 99,
  license: 'Kurumsal — 250 kullanıcı',
  uptime: '%99,98'
};

const CUST_KEY = 'gms.admin.customers';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly customers = signal<AdminCustomer[]>(this.loadCustomers());
  private custSeq = 4;

  getSystemInfo(): Observable<SystemInfo> {
    return of(SYSTEM_INFO);
  }
  getCustomers(): Observable<AdminCustomer[]> {
    return of(this.customers());
  }
  getProjects(): Observable<AdminProject[]> {
    return of(PROJECTS);
  }
  getEnvironments(): Observable<AdminEnvironment[]> {
    return of(ENVIRONMENTS);
  }
  getUsers(): Observable<AdminUser[]> {
    return of(USERS);
  }
  getRoles(): Observable<AdminRole[]> {
    return of(ROLES);
  }

  createCustomer(name: string): Observable<AdminCustomer> {
    const n = String(++this.custSeq).padStart(3, '0');
    const created: AdminCustomer = {
      id: 'c-' + this.custSeq,
      code: `CUST-${n}`,
      name,
      status: 'Active',
      projectCount: 0,
      createdAt: new Date().toISOString()
    };
    this.customers.update((list) => [created, ...list]);
    this.persistCustomers();
    return of(created);
  }

  private loadCustomers(): AdminCustomer[] {
    try {
      const raw = localStorage.getItem(CUST_KEY);
      return raw ? (JSON.parse(raw) as AdminCustomer[]) : CUSTOMERS;
    } catch {
      return CUSTOMERS;
    }
  }
  private persistCustomers(): void {
    try {
      localStorage.setItem(CUST_KEY, JSON.stringify(this.customers()));
    } catch {
      /* ignore */
    }
  }
}
