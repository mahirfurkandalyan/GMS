import { Injectable, signal } from '@angular/core';
import { Observable, of } from 'rxjs';
import { IconName } from '../shared/icon/icon';

/**
 * Asset Center data layer — the CMDB foundation of GMS. Every Release, Change,
 * Validation and Execution eventually references one or more Assets.
 *
 * Frontend-only, observable-based (`of()`) so a real CMDB/API can replace the
 * service body without touching components. No dependency graph / impact / AI
 * engine is implemented here — only the reusable data shape that will feed them.
 */

/** Asset type catalog — the governed object taxonomy. UI reads icons/labels from here. */
export interface AssetType {
  key: string;
  label: string;
  icon: IconName;
}

export const ASSET_TYPES: AssetType[] = [
  { key: 'database', label: 'Veritabanı', icon: 'server' },
  { key: 'table', label: 'Tablo', icon: 'grid' },
  { key: 'view', label: 'Görünüm', icon: 'grid' },
  { key: 'stored-procedure', label: 'Saklı Yordam', icon: 'server' },
  { key: 'function', label: 'Fonksiyon', icon: 'change' },
  { key: 'api', label: 'API', icon: 'share' },
  { key: 'service', label: 'Servis', icon: 'hub' },
  { key: 'application', label: 'Uygulama', icon: 'dashboard' },
  { key: 'microservice', label: 'Mikroservis', icon: 'hub' },
  { key: 'configuration', label: 'Konfigürasyon', icon: 'filter' },
  { key: 'message-queue', label: 'Mesaj Kuyruğu', icon: 'inbox' },
  { key: 'report', label: 'Rapor', icon: 'document' },
  { key: 'file', label: 'Dosya', icon: 'document' },
  { key: 'document', label: 'Doküman', icon: 'document' },
  { key: 'infrastructure', label: 'Altyapı', icon: 'server' },
  { key: 'other', label: 'Diğer', icon: 'folder' }
];

export function assetTypeLabel(key: string): string {
  return ASSET_TYPES.find((t) => t.key === key)?.label ?? key;
}
export function assetTypeIcon(key: string): IconName {
  return ASSET_TYPES.find((t) => t.key === key)?.icon ?? 'folder';
}

/** Criticality → maps onto the shared PRIORITY_BADGES registry. */
export const ASSET_CRITICALITIES = ['Critical', 'High', 'Medium', 'Low'];
export const ASSET_STATUSES = ['Active', 'Inactive', 'Deprecated', 'Archived'];
export const ASSET_ENVIRONMENTS = ['DEV', 'TEST', 'UAT', 'PROD'];

/** How one asset relates to another — prepares the future dependency graph. */
export type DependencyRelation = 'depends-on' | 'used-by' | 'connects-to' | 'contains';

export const DEPENDENCY_RELATIONS: Record<DependencyRelation, string> = {
  'depends-on': 'Bağımlı',
  'used-by': 'Kullanan',
  'connects-to': 'Bağlantılı',
  'contains': 'İçerir'
};

/** Lightweight reference to another governance object. */
export interface AssetRef {
  id: string;
  code: string;
  name: string;
  route: string;
}

/** Edge in the (future) dependency graph — kept generic on purpose. */
export interface AssetDependency {
  id: string;
  code: string;
  name: string;
  type: string; // asset type key
  relation: DependencyRelation;
}

export interface GmsAsset {
  id: string;
  code: string; // Asset ID, e.g. AST-2026-001
  name: string;
  type: string; // asset type key
  projectId: string;
  projectName: string;
  environment: string; // DEV | TEST | UAT | PROD
  owner: string;
  criticality: string; // Critical | High | Medium | Low
  status: string; // Active | Inactive | Deprecated | Archived
  description: string;
  createdAt: string;
  updatedAt: string;
  // Relationship payload — reusable across Relationships / Releases / Changes / Documents tabs.
  releases: AssetRef[];
  changes: AssetRef[];
  documents: AssetRef[];
  dependencies: AssetDependency[];
}

export interface CreateAssetPayload {
  name: string;
  type: string;
  projectId: string;
  projectName: string;
  environment: string;
  owner: string;
  criticality: string;
  description: string;
}

const KEY = 'gms.assets';

function rel(id: string, code: string, name: string): AssetRef {
  return { id, code, name, route: `/releases/${id}` };
}
function chg(id: string, code: string, name: string): AssetRef {
  return { id, code, name, route: `/changes/${id}` };
}
function doc(id: string, code: string, name: string): AssetRef {
  return { id, code, name, route: `/documents/${id}` };
}
function dep(id: string, code: string, name: string, type: string, relation: DependencyRelation): AssetDependency {
  return { id, code, name, type, relation };
}

const REL_001 = '07777777-7777-7777-7777-777777777701';
const REL_003 = 'a0000000-0000-0000-0000-000000000003';
const REL_004 = 'a0000000-0000-0000-0000-000000000004';
const REL_006 = 'a0000000-0000-0000-0000-000000000006';

const SEED: GmsAsset[] = [
  {
    id: 'ast-2026-001', code: 'AST-2026-001', name: 'EBR Üretim Veritabanı', type: 'database',
    projectId: 'p1', projectName: 'EBR Migration', environment: 'PROD', owner: 'Ayşe Yılmaz',
    criticality: 'Critical', status: 'Active',
    description: 'Elektronik Batch Record üretim ortamı ana veritabanı. Tüm parti kayıtlarının birincil deposu.',
    createdAt: '2025-11-02T09:00:00', updatedAt: '2026-03-05T11:00:00',
    releases: [rel(REL_001, 'REL-2026-001', 'EBR Migration v1'), rel(REL_003, 'REL-2026-003', 'EBR Şema Revizyonu')],
    changes: [chg('chg-2026-014', 'CHG-2026-014', 'Şema güncelleme değişikliği')],
    documents: [doc('doc-2026-002', 'DOC-2026-002', 'Şema Güncelleme Betiği'), doc('doc-2026-003', 'DOC-2026-003', 'Geri Alma Betiği')],
    dependencies: [
      dep('ast-2026-002', 'AST-2026-002', 'batch_record Tablosu', 'table', 'contains'),
      dep('ast-2026-004', 'AST-2026-004', 'EBR Batch API', 'api', 'used-by'),
      dep('ast-2026-007', 'AST-2026-007', 'Yedekleme Altyapısı', 'infrastructure', 'connects-to')
    ]
  },
  {
    id: 'ast-2026-002', code: 'AST-2026-002', name: 'batch_record Tablosu', type: 'table',
    projectId: 'p1', projectName: 'EBR Migration', environment: 'PROD', owner: 'Ayşe Yılmaz',
    criticality: 'High', status: 'Active',
    description: 'Parti kayıtlarını tutan ana tablo. reviewed_by kolonu CHG-2026-014 ile eklendi.',
    createdAt: '2025-11-02T09:10:00', updatedAt: '2026-03-05T11:05:00',
    releases: [rel(REL_001, 'REL-2026-001', 'EBR Migration v1')],
    changes: [chg('chg-2026-014', 'CHG-2026-014', 'Şema güncelleme değişikliği')],
    documents: [doc('doc-2026-002', 'DOC-2026-002', 'Şema Güncelleme Betiği')],
    dependencies: [
      dep('ast-2026-001', 'AST-2026-001', 'EBR Üretim Veritabanı', 'database', 'depends-on'),
      dep('ast-2026-003', 'AST-2026-003', 'Parti Onay Yordamı', 'stored-procedure', 'used-by')
    ]
  },
  {
    id: 'ast-2026-003', code: 'AST-2026-003', name: 'Parti Onay Yordamı', type: 'stored-procedure',
    projectId: 'p1', projectName: 'EBR Migration', environment: 'UAT', owner: 'Ali Vural',
    criticality: 'Medium', status: 'Active',
    description: 'Parti kayıtlarını REVIEWED durumuna geçiren saklı yordam.',
    createdAt: '2026-01-12T10:00:00', updatedAt: '2026-02-28T16:00:00',
    releases: [rel(REL_003, 'REL-2026-003', 'EBR Şema Revizyonu')],
    changes: [chg('chg-2026-014', 'CHG-2026-014', 'Şema güncelleme değişikliği')],
    documents: [],
    dependencies: [dep('ast-2026-002', 'AST-2026-002', 'batch_record Tablosu', 'table', 'depends-on')]
  },
  {
    id: 'ast-2026-004', code: 'AST-2026-004', name: 'EBR Batch API', type: 'api',
    projectId: 'p1', projectName: 'EBR Migration', environment: 'PROD', owner: 'Furkan Demir',
    criticality: 'Critical', status: 'Active',
    description: 'Parti kayıtlarına erişim sağlayan REST API. MES entegrasyonunun ana giriş noktası.',
    createdAt: '2025-12-01T09:00:00', updatedAt: '2026-03-01T09:00:00',
    releases: [rel(REL_001, 'REL-2026-001', 'EBR Migration v1'), rel(REL_004, 'REL-2026-004', 'MES Upgrade')],
    changes: [chg('chg-2026-017', 'CHG-2026-017', 'API sürüm yükseltme')],
    documents: [doc('doc-2026-006', 'DOC-2026-006', 'Mimari Tasarım')],
    dependencies: [
      dep('ast-2026-001', 'AST-2026-001', 'EBR Üretim Veritabanı', 'database', 'depends-on'),
      dep('ast-2026-005', 'AST-2026-005', 'MES Entegrasyon Servisi', 'microservice', 'connects-to'),
      dep('ast-2026-008', 'AST-2026-008', 'Kimlik Doğrulama Konfigürasyonu', 'configuration', 'depends-on')
    ]
  },
  {
    id: 'ast-2026-005', code: 'AST-2026-005', name: 'MES Entegrasyon Servisi', type: 'microservice',
    projectId: 'p2', projectName: 'MES Upgrade', environment: 'UAT', owner: 'Furkan Demir',
    criticality: 'High', status: 'Active',
    description: 'MES ile GMS arasında veri senkronizasyonu sağlayan mikroservis.',
    createdAt: '2026-01-20T09:00:00', updatedAt: '2026-03-11T10:00:00',
    releases: [rel(REL_004, 'REL-2026-004', 'MES Upgrade')],
    changes: [chg('chg-2026-017', 'CHG-2026-017', 'API sürüm yükseltme')],
    documents: [doc('doc-2026-005', 'DOC-2026-005', 'UAT Test Kanıtı')],
    dependencies: [
      dep('ast-2026-004', 'AST-2026-004', 'EBR Batch API', 'api', 'connects-to'),
      dep('ast-2026-006', 'AST-2026-006', 'Olay Mesaj Kuyruğu', 'message-queue', 'used-by')
    ]
  },
  {
    id: 'ast-2026-006', code: 'AST-2026-006', name: 'Olay Mesaj Kuyruğu', type: 'message-queue',
    projectId: 'p2', projectName: 'MES Upgrade', environment: 'TEST', owner: 'Mehmet Kaya',
    criticality: 'Medium', status: 'Active',
    description: 'Servisler arası asenkron olay iletişimi için mesaj kuyruğu.',
    createdAt: '2026-01-25T09:00:00', updatedAt: '2026-02-20T14:00:00',
    releases: [rel(REL_004, 'REL-2026-004', 'MES Upgrade')],
    changes: [],
    documents: [],
    dependencies: [dep('ast-2026-005', 'AST-2026-005', 'MES Entegrasyon Servisi', 'microservice', 'used-by')]
  },
  {
    id: 'ast-2026-007', code: 'AST-2026-007', name: 'Yedekleme Altyapısı', type: 'infrastructure',
    projectId: 'p1', projectName: 'EBR Migration', environment: 'PROD', owner: 'System Administrator',
    criticality: 'High', status: 'Active',
    description: 'Üretim veritabanları için otomatik yedekleme ve felaket kurtarma altyapısı.',
    createdAt: '2025-10-15T09:00:00', updatedAt: '2026-01-30T09:00:00',
    releases: [],
    changes: [],
    documents: [],
    dependencies: [dep('ast-2026-001', 'AST-2026-001', 'EBR Üretim Veritabanı', 'database', 'connects-to')]
  },
  {
    id: 'ast-2026-008', code: 'AST-2026-008', name: 'Kimlik Doğrulama Konfigürasyonu', type: 'configuration',
    projectId: 'p2', projectName: 'MES Upgrade', environment: 'PROD', owner: 'Furkan Demir',
    criticality: 'Critical', status: 'Active',
    description: 'OAuth2 tabanlı kimlik doğrulama ve yetkilendirme yapılandırması.',
    createdAt: '2025-12-10T09:00:00', updatedAt: '2026-02-15T09:00:00',
    releases: [rel(REL_006, 'REL-2026-006', 'Güvenlik Yaması')],
    changes: [chg('chg-2026-019', 'CHG-2026-019', 'Güvenlik yaması uygulama')],
    documents: [doc('doc-2026-008', 'DOC-2026-008', 'Onay Kanıtı')],
    dependencies: [dep('ast-2026-004', 'AST-2026-004', 'EBR Batch API', 'api', 'used-by')]
  },
  {
    id: 'ast-2026-009', code: 'AST-2026-009', name: 'Kalite Raporlama Uygulaması', type: 'application',
    projectId: 'p1', projectName: 'EBR Migration', environment: 'TEST', owner: 'Mehmet Kaya',
    criticality: 'Medium', status: 'Inactive',
    description: 'Kalite metriklerini görselleştiren dahili raporlama uygulaması.',
    createdAt: '2025-09-01T09:00:00', updatedAt: '2026-01-10T09:00:00',
    releases: [],
    changes: [],
    documents: [doc('doc-2026-007', 'DOC-2026-007', 'Yürütme Raporu')],
    dependencies: [dep('ast-2026-004', 'AST-2026-004', 'EBR Batch API', 'api', 'depends-on')]
  },
  {
    id: 'ast-2026-010', code: 'AST-2026-010', name: 'Eski Denetim Görünümü', type: 'view',
    projectId: 'p1', projectName: 'EBR Migration', environment: 'DEV', owner: 'Ali Vural',
    criticality: 'Low', status: 'Deprecated',
    description: 'Denetim kayıtları için eski birleştirilmiş görünüm. Yeni denetim tablosuyla değiştirildi.',
    createdAt: '2025-06-01T09:00:00', updatedAt: '2025-12-01T09:00:00',
    releases: [],
    changes: [],
    documents: [],
    dependencies: [dep('ast-2026-002', 'AST-2026-002', 'batch_record Tablosu', 'table', 'depends-on')]
  },
  {
    id: 'ast-2026-011', code: 'AST-2026-011', name: 'Parti İzlenebilirlik Fonksiyonu', type: 'function',
    projectId: 'p1', projectName: 'EBR Migration', environment: 'UAT', owner: 'Ayşe Yılmaz',
    criticality: 'Medium', status: 'Active',
    description: 'Parti geçmişini geriye doğru izleyen SQL fonksiyonu.',
    createdAt: '2026-02-01T09:00:00', updatedAt: '2026-02-25T09:00:00',
    releases: [rel(REL_003, 'REL-2026-003', 'EBR Şema Revizyonu')],
    changes: [chg('chg-2026-014', 'CHG-2026-014', 'Şema güncelleme değişikliği')],
    documents: [],
    dependencies: [dep('ast-2026-002', 'AST-2026-002', 'batch_record Tablosu', 'table', 'depends-on')]
  },
  {
    id: 'ast-2026-012', code: 'AST-2026-012', name: 'Arşivlenmiş Raporlama Servisi', type: 'service',
    projectId: 'p2', projectName: 'MES Upgrade', environment: 'DEV', owner: 'System Administrator',
    criticality: 'Low', status: 'Archived',
    description: 'Kullanımdan kaldırılan eski raporlama servisi. Yalnızca tarihsel referans için tutulur.',
    createdAt: '2025-05-01T09:00:00', updatedAt: '2025-11-01T09:00:00',
    releases: [],
    changes: [],
    documents: [],
    dependencies: []
  }
];

@Injectable({ providedIn: 'root' })
export class AssetService {
  private readonly store = signal<GmsAsset[]>(this.load());
  private seq = 12;

  getAssets(): Observable<GmsAsset[]> {
    return of(this.store());
  }

  getAsset(id: string): Observable<GmsAsset | undefined> {
    return of(this.store().find((a) => a.id === id));
  }

  create(payload: CreateAssetPayload): Observable<GmsAsset> {
    const n = String(++this.seq).padStart(3, '0');
    const now = new Date().toISOString();
    const asset: GmsAsset = {
      id: 'ast-' + this.seq,
      code: `AST-2026-${n}`,
      status: 'Active',
      createdAt: now,
      updatedAt: now,
      releases: [],
      changes: [],
      documents: [],
      dependencies: [],
      ...payload
    };
    this.store.update((list) => [asset, ...list]);
    this.persist();
    return of(asset);
  }

  private load(): GmsAsset[] {
    try {
      const raw = localStorage.getItem(KEY);
      return raw ? (JSON.parse(raw) as GmsAsset[]) : SEED;
    } catch {
      return SEED;
    }
  }

  private persist(): void {
    try {
      localStorage.setItem(KEY, JSON.stringify(this.store()));
    } catch {
      /* ignore */
    }
  }
}
