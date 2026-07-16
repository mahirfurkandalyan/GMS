import { Injectable, signal } from '@angular/core';
import { Observable, of } from 'rxjs';
import { IconName } from '../shared/icon/icon';

export type RiskLevel = 'Critical' | 'High' | 'Medium' | 'Low';
export type PriorityLevel = 'Critical' | 'High' | 'Medium' | 'Low';
export type ChangeClass = 'Standard' | 'Normal' | 'Emergency';

/**
 * LEGACY mock Change store (localStorage). As of the Change/Workflow backend integration sprint the
 * Change screens (list / detail / wizard) NO LONGER use this — they talk to the real backend via
 * ChangeApiService. This mock now survives ONLY as an interim data source for modules that are
 * intentionally NOT migrated yet (Release Planning wizard, Reports). It will be removed when those
 * modules are integrated. Do not wire new Change/Workflow feature code to this service.
 *
 * The exported CHANGE_CLASSES / CHANGE_TYPES catalogs and the RiskLevel/ChangeClass/AffectedAssetRef
 * types below are plain UI catalog/types (not persistence) and are still shared by the wizard/vm.
 */

export const CHANGE_CLASSES: { key: ChangeClass; label: string; description: string }[] = [
  { key: 'Standard', label: 'Standart Değişiklik', description: 'Önceden onaylanmış, düşük riskli, tekrarlayan değişiklik.' },
  { key: 'Normal', label: 'Normal Değişiklik', description: 'Tam onay ve değerlendirme süreci gerektiren değişiklik.' },
  { key: 'Emergency', label: 'Acil Değişiklik', description: 'Acil müdahale gerektiren, hızlandırılmış onay akışı.' }
];
export function changeClassLabel(key: string): string {
  return CHANGE_CLASSES.find((c) => c.key === key)?.label ?? key;
}

export interface ChangeTypeMeta { key: string; label: string; icon: IconName; }
export const CHANGE_TYPES: ChangeTypeMeta[] = [
  { key: 'app-deploy', label: 'Uygulama Dağıtımı', icon: 'dashboard' },
  { key: 'db-schema', label: 'Veritabanı Şema Değişikliği', icon: 'server' },
  { key: 'sql-fix', label: 'SQL Veri Düzeltmesi', icon: 'server' },
  { key: 'sp-func', label: 'Saklı Yordam / Fonksiyon Değişikliği', icon: 'server' },
  { key: 'api', label: 'API Değişikliği', icon: 'share' },
  { key: 'config', label: 'Konfigürasyon Değişikliği', icon: 'filter' },
  { key: 'infra', label: 'Altyapı Değişikliği', icon: 'server' },
  { key: 'integration', label: 'Entegrasyon Değişikliği', icon: 'hub' },
  { key: 'doc-sop', label: 'Doküman / SOP Değişikliği', icon: 'document' },
  { key: 'other', label: 'Diğer', icon: 'change' }
];
export function changeTypeLabel(key: string): string {
  return CHANGE_TYPES.find((c) => c.key === key)?.label ?? key;
}
export function changeTypeIcon(key: string): IconName {
  return CHANGE_TYPES.find((c) => c.key === key)?.icon ?? 'change';
}

export const CHANGE_STATUSES = ['Draft', 'Submitted', 'InReview', 'Approval', 'Approved', 'Rejected', 'Executing', 'Completed', 'Cancelled'];

/** Reference to an affected asset — mirrors the Asset Center model. */
export interface AffectedAssetRef {
  id: string;
  code: string;
  name: string;
  type: string; // asset type key
  criticality: string;
  environment: string;
}

/** A selected supporting document category (no file upload yet). */
export interface ChangeDocRef {
  category: string; // document type key
}

export interface Change {
  id: string;
  code: string;
  title: string;
  changeClass: ChangeClass;
  changeType: string; // technical type key
  risk: RiskLevel; // auto-calculated, never manually chosen
  priority: PriorityLevel;
  status: string;
  owner: string;
  // Governance linkage (release assigned later during Release Planning).
  releaseId: string;
  releaseCode: string;
  customerName: string;
  projectName: string;
  environmentName: string;
  // General information
  businessReason: string;
  description: string;
  sourceSystem: string;
  sourceReference: string;
  plannedDate: string | null;
  // Step 2/3/4 payloads
  technical: Record<string, string | boolean>;
  assets: AffectedAssetRef[];
  documents: ChangeDocRef[];
  createdBy: string;
  createdAt: string;
  updatedAt: string;
}

/** Everything the wizard collects (server fields added on create). */
export interface ChangeInput {
  title: string;
  changeClass: ChangeClass;
  changeType: string;
  risk: RiskLevel;
  priority: PriorityLevel;
  customerName: string;
  projectName: string;
  environmentName: string;
  releaseId: string;
  releaseCode: string;
  businessReason: string;
  description: string;
  sourceSystem: string;
  sourceReference: string;
  plannedDate: string | null;
  technical: Record<string, string | boolean>;
  assets: AffectedAssetRef[];
  documents: ChangeDocRef[];
  owner: string;
}

const KEY = 'gms.changes';

function ch(
  code: string, title: string, changeClass: ChangeClass, changeType: string, risk: RiskLevel, priority: PriorityLevel,
  status: string, owner: string, releaseId: string, releaseCode: string, projectName: string, environmentName: string,
  customerName: string, businessReason: string, sourceReference: string, createdAt: string, updatedAt: string,
  extra: Partial<Change> = {}
): Change {
  return {
    id: code.toLowerCase(), code, title, changeClass, changeType, risk, priority, status, owner,
    releaseId, releaseCode, projectName, environmentName, customerName,
    businessReason, description: '', sourceSystem: 'JIRA', sourceReference, plannedDate: null,
    technical: {}, assets: [], documents: [],
    createdBy: owner, createdAt, updatedAt,
    ...extra
  };
}

const SEED: Change[] = [
  ch('CHG-2026-014', 'Veritabanı şeması güncellemesi', 'Standard', 'db-schema', 'Medium', 'High', 'InReview', 'Ayşe Yılmaz', '07777777-7777-7777-7777-777777777701', 'REL-2026-001', 'EBR Migration', 'PROD', 'Abdi İbrahim', 'Performans iyileştirmesi için şema optimizasyonu gereklidir.', 'JIRA-1042', '2026-03-01T09:20:00', '2026-03-04T14:10:00', {
    description: 'batch_record tablosuna reviewed_by kolonu eklenecek ve indeksler yeniden düzenlenecek.',
    plannedDate: '2026-07-15T10:00:00',
    technical: { affectedDatabase: 'EBR_PROD', affectedSchema: 'dbo', transactionUsed: true, estimatedRowCount: '1.2M', backupRequired: true },
    assets: [
      { id: 'ast-2026-001', code: 'AST-2026-001', name: 'EBR Üretim Veritabanı', type: 'database', criticality: 'Critical', environment: 'PROD' },
      { id: 'ast-2026-002', code: 'AST-2026-002', name: 'batch_record Tablosu', type: 'table', criticality: 'High', environment: 'PROD' }
    ],
    documents: [{ category: 'sql-script' }, { category: 'rollback-script' }]
  }),
  ch('CHG-2026-015', 'Yetki matrisi revizyonu', 'Emergency', 'config', 'High', 'Critical', 'Approval', 'Furkan Demir', '07777777-7777-7777-7777-777777777701', 'REL-2026-001', 'EBR Migration', 'PROD', 'Abdi İbrahim', 'Denetim bulgusu sonrası yetki matrisinin güncellenmesi.', 'JIRA-1058', '2026-03-02T11:00:00', '2026-03-05T16:30:00', {
    technical: { configArea: 'Yetkilendirme', oldValue: 'role_matrix_v2', newValue: 'role_matrix_v3', restartRequired: true, downtimeExpected: true }
  }),
  ch('CHG-2026-016', 'Rapor şablonu ekleme', 'Standard', 'doc-sop', 'Low', 'Low', 'Draft', 'Mehmet Kaya', 'a0000000-0000-0000-0000-000000000003', 'REL-2026-003', 'EBR Migration', 'UAT', 'Abdi İbrahim', 'Yeni kalite raporu şablonu talebi.', 'JIRA-1063', '2026-03-06T10:15:00', '2026-03-06T10:15:00'),
  ch('CHG-2026-017', 'API uç noktası kullanımdan kaldırma', 'Normal', 'api', 'Medium', 'Medium', 'Approved', 'Ali Vural', 'a0000000-0000-0000-0000-000000000004', 'REL-2026-004', 'MES Upgrade', 'TEST', 'Bilim İlaç', 'Eski API uç noktalarının kademeli kaldırılması.', 'JIRA-1071', '2026-03-08T13:40:00', '2026-03-12T09:05:00', {
    technical: { apiName: 'Batch API', endpoint: '/api/v1/batch', httpMethod: 'GET', version: 'v1', backwardCompatible: false, swaggerUrl: 'https://api.gms.local/swagger' },
    assets: [{ id: 'ast-2026-004', code: 'AST-2026-004', name: 'EBR Batch API', type: 'api', criticality: 'Critical', environment: 'PROD' }]
  }),
  ch('CHG-2026-018', 'Kullanıcı arayüzü güncellemesi', 'Standard', 'app-deploy', 'Low', 'Medium', 'Executing', 'Elif Aydın', 'a0000000-0000-0000-0000-000000000005', 'REL-2026-005', 'EBR Migration', 'PROD', 'Abdi İbrahim', 'Operatör ekranlarında kullanılabilirlik iyileştirmesi.', 'JIRA-1080', '2026-03-10T08:30:00', '2026-03-15T17:20:00', {
    technical: { repository: 'gms-ui', branch: 'release/1.2', buildNumber: '2451', artifactVersion: 'v1.2.0', estimatedDuration: '30 dk' }
  }),
  ch('CHG-2026-019', 'Güvenlik yaması uygulaması', 'Emergency', 'infra', 'High', 'High', 'Completed', 'Ali Vural', 'a0000000-0000-0000-0000-000000000006', 'REL-2026-006', 'MES Upgrade', 'PROD', 'Bilim İlaç', 'Kritik güvenlik açığının kapatılması.', 'JIRA-1090', '2026-02-20T09:00:00', '2026-02-28T18:00:00', {
    technical: { server: 'mes-app-01', operatingSystem: 'Windows Server 2022', affectedServices: 'MES API, MES Worker', maintenanceWindow: '02:00 - 04:00' },
    assets: [{ id: 'ast-2026-008', code: 'AST-2026-008', name: 'Kimlik Doğrulama Konfigürasyonu', type: 'configuration', criticality: 'Critical', environment: 'PROD' }]
  }),
  // Approved changes not yet assigned to a release (releaseId '') — the backlog the
  // Release Planning Wizard draws from. A release is always built from these.
  ch('CHG-2026-020', 'Ödeme servisi indeks optimizasyonu', 'Normal', 'db-schema', 'Medium', 'High', 'Approved', 'Ayşe Yılmaz', '', '—', 'EBR Migration', 'PROD', 'Abdi İbrahim', 'Ödeme sorgularında yavaşlık için indeks düzenlemesi.', 'JIRA-1101', '2026-06-20T09:00:00', '2026-06-28T14:00:00', {
    plannedDate: '2026-08-01T10:00:00',
    assets: [{ id: 'ast-2026-002', code: 'AST-2026-002', name: 'batch_record Tablosu', type: 'table', criticality: 'High', environment: 'PROD' }],
    documents: [{ category: 'sql-script' }, { category: 'rollback-script' }]
  }),
  ch('CHG-2026-021', 'Batch API v2 yayını', 'Normal', 'api', 'Medium', 'Medium', 'Approved', 'Furkan Demir', '', '—', 'EBR Migration', 'PROD', 'Abdi İbrahim', 'Batch API v2 geriye dönük uyumlu sürüm.', 'JIRA-1102', '2026-06-22T09:00:00', '2026-06-29T11:00:00', {
    plannedDate: '2026-08-01T10:00:00',
    assets: [{ id: 'ast-2026-004', code: 'AST-2026-004', name: 'EBR Batch API', type: 'api', criticality: 'Critical', environment: 'PROD' }],
    documents: [{ category: 'release-note' }]
  }),
  ch('CHG-2026-022', 'Operatör ekranı yeniden tasarımı', 'Standard', 'app-deploy', 'Low', 'Medium', 'Approved', 'Elif Aydın', '', '—', 'EBR Migration', 'UAT', 'Abdi İbrahim', 'Operatör ekranlarının kullanılabilirlik güncellemesi.', 'JIRA-1103', '2026-06-24T09:00:00', '2026-06-30T15:00:00', {
    plannedDate: '2026-08-05T10:00:00',
    documents: [{ category: 'test-evidence' }]
  }),
  ch('CHG-2026-023', 'Kimlik doğrulama yaması', 'Emergency', 'config', 'High', 'Critical', 'Approved', 'Ali Vural', '', '—', 'MES Upgrade', 'PROD', 'Bilim İlaç', 'OAuth yapılandırmasında acil güvenlik düzeltmesi.', 'JIRA-1104', '2026-06-25T09:00:00', '2026-07-01T09:00:00', {
    plannedDate: '2026-07-20T10:00:00',
    technical: { configArea: 'Kimlik Doğrulama', downtimeExpected: true },
    assets: [{ id: 'ast-2026-008', code: 'AST-2026-008', name: 'Kimlik Doğrulama Konfigürasyonu', type: 'configuration', criticality: 'Critical', environment: 'PROD' }],
    documents: [{ category: 'rollback-script' }, { category: 'approval-evidence' }]
  })
];

@Injectable({ providedIn: 'root' })
export class ChangeService {
  private readonly store = signal<Change[]>(this.load());
  private seq = 100;

  getChanges(): Observable<Change[]> {
    return of(this.store());
  }

  getChange(id: string): Observable<Change | undefined> {
    return of(this.store().find((c) => c.id === id));
  }

  /** Create a change from the wizard. `status` is 'Draft' (Save Draft) or 'Submitted'. */
  create(input: ChangeInput, status: 'Draft' | 'Submitted' = 'Draft'): Observable<Change> {
    const n = String(20 + (this.seq++ % 900)).padStart(3, '0');
    const now = new Date().toISOString();
    const change: Change = {
      id: 'chg-' + this.seq,
      code: `CHG-2026-${n}`,
      status,
      createdBy: input.owner,
      createdAt: now,
      updatedAt: now,
      ...input
    };
    this.store.update((list) => [change, ...list]);
    this.persist();
    return of(change);
  }

  private load(): Change[] {
    try {
      const raw = localStorage.getItem(KEY);
      return raw ? (JSON.parse(raw) as Change[]) : SEED;
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
