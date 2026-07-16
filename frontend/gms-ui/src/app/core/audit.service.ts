import { Injectable, signal } from '@angular/core';
import { Observable, of } from 'rxjs';
import { IconName } from '../shared/icon/icon';
import { BadgeTone } from '../shared/ui/badge/badge';

/**
 * Governance Audit Center data layer. Immutable audit trail of every critical
 * action performed in GMS. Frontend-only, observable-based (`of()`) so a real
 * audit/SIEM API can replace the service body without touching components.
 * No backend audit engine, retention policy or security integration here.
 */

export type AuditAction =
  | 'Create' | 'Update' | 'Delete' | 'Approve' | 'Reject' | 'Validate'
  | 'Execute' | 'Rollback' | 'Login' | 'Logout' | 'Export' | 'Import';

export type AuditResult = 'Success' | 'Failed' | 'Warning';

/** Which left-nav bucket a record belongs to. */
export type AuditCategory = 'record' | 'user' | 'security' | 'compliance';

export interface AuditActionMeta {
  label: string;
  tone: BadgeTone;
  icon: IconName;
}

export const ACTION_META: Record<AuditAction, AuditActionMeta> = {
  Create: { label: 'Oluşturma', tone: 'success', icon: 'plus' },
  Update: { label: 'Güncelleme', tone: 'info', icon: 'change' },
  Delete: { label: 'Silme', tone: 'danger', icon: 'close' },
  Approve: { label: 'Onaylama', tone: 'success', icon: 'approval' },
  Reject: { label: 'Reddetme', tone: 'danger', icon: 'close' },
  Validate: { label: 'Doğrulama', tone: 'info', icon: 'shield' },
  Execute: { label: 'Yürütme', tone: 'warning', icon: 'execution' },
  Rollback: { label: 'Geri Alma', tone: 'danger', icon: 'change' },
  Login: { label: 'Giriş', tone: 'neutral', icon: 'user' },
  Logout: { label: 'Çıkış', tone: 'neutral', icon: 'logout' },
  Export: { label: 'Dışa Aktarma', tone: 'neutral', icon: 'document' },
  Import: { label: 'İçe Aktarma', tone: 'info', icon: 'inbox' }
};

export const RESULT_META: Record<AuditResult, { label: string; tone: BadgeTone }> = {
  Success: { label: 'Başarılı', tone: 'success' },
  Failed: { label: 'Başarısız', tone: 'danger' },
  Warning: { label: 'Uyarı', tone: 'warning' }
};

export const AUDIT_ACTIONS: AuditAction[] = [
  'Create', 'Update', 'Delete', 'Approve', 'Reject', 'Validate',
  'Execute', 'Rollback', 'Login', 'Logout', 'Export', 'Import'
];

export const AUDIT_MODULES = ['Yayın', 'Değişiklik', 'Onay', 'Yürütme', 'Doğrulama', 'Doküman', 'Varlık', 'Yönetim', 'Kimlik'];
export const AUDIT_OBJECT_TYPES = ['Release', 'Change', 'Approval', 'Execution', 'Validation', 'Document', 'Asset', 'User', 'Customer', 'Session'];

export type FieldChangeType = 'added' | 'modified' | 'removed' | 'unchanged';

export const CHANGE_TYPE_META: Record<FieldChangeType, { label: string; tone: BadgeTone }> = {
  added: { label: 'Eklendi', tone: 'success' },
  modified: { label: 'Değiştirildi', tone: 'warning' },
  removed: { label: 'Kaldırıldı', tone: 'danger' },
  unchanged: { label: 'Değişmedi', tone: 'neutral' }
};

export interface AuditFieldChange {
  field: string;
  previous: string;
  current: string;
  changeType: FieldChangeType;
}

export interface AuditRelatedRef {
  id: string;
  code: string;
  name: string;
  route: string;
  kind: 'release' | 'change' | 'document';
}

export interface AuditRecord {
  id: string;
  code: string; // AUD-2026-0001
  timestamp: string;
  user: string;
  userRole: string;
  module: string;
  objectType: string;
  objectName: string;
  objectId: string;
  action: AuditAction;
  previousPreview: string;
  currentPreview: string;
  environment: string;
  result: AuditResult;
  category: AuditCategory;
  ip: string;
  description: string;
  fields: AuditFieldChange[];
  related: AuditRelatedRef[];
}

export function actionMeta(a: string): AuditActionMeta {
  return ACTION_META[a as AuditAction] ?? { label: a, tone: 'neutral', icon: 'audit' };
}
export function resultMeta(r: string): { label: string; tone: BadgeTone } {
  return RESULT_META[r as AuditResult] ?? { label: r, tone: 'neutral' };
}

function relRef(id: string, code: string, name: string): AuditRelatedRef {
  return { id, code, name, route: `/releases/${id}`, kind: 'release' };
}
function chgRef(id: string, code: string, name: string): AuditRelatedRef {
  return { id, code, name, route: `/changes/${id}`, kind: 'change' };
}
function docRef(id: string, code: string, name: string): AuditRelatedRef {
  return { id, code, name, route: `/documents/${id}`, kind: 'document' };
}

const REL_001 = '07777777-7777-7777-7777-777777777701';
const REL_006 = 'a0000000-0000-0000-0000-000000000006';

let seq = 0;
function rec(
  ts: string, user: string, role: string, module: string, objectType: string, objectName: string, objectId: string,
  action: AuditAction, prev: string, curr: string, env: string, result: AuditResult, category: AuditCategory,
  description: string, fields: AuditFieldChange[], related: AuditRelatedRef[]
): AuditRecord {
  seq++;
  return {
    id: `aud-${String(seq).padStart(4, '0')}`,
    code: `AUD-2026-${String(seq).padStart(4, '0')}`,
    timestamp: ts, user, userRole: role, module, objectType, objectName, objectId, action,
    previousPreview: prev, currentPreview: curr, environment: env, result, category,
    ip: '10.20.' + (30 + (seq % 12)) + '.' + (100 + seq),
    description, fields, related
  };
}

const SEED: AuditRecord[] = [
  rec('2026-07-06T09:32:14', 'Ayşe Yılmaz', 'Architect', 'Değişiklik', 'Change', 'CHG-2026-014', 'chg-2026-014', 'Update',
    'status: InReview', 'status: Approval', 'PROD', 'Success', 'record',
    'Şema güncelleme değişikliğinin durumu onaya taşındı.',
    [
      { field: 'status', previous: 'InReview', current: 'Approval', changeType: 'modified' },
      { field: 'reviewer', previous: '—', current: 'Mehmet Kaya', changeType: 'added' },
      { field: 'updatedAt', previous: '2026-07-05', current: '2026-07-06', changeType: 'modified' }
    ],
    [relRef(REL_001, 'REL-2026-001', 'EBR Migration v1'), chgRef('chg-2026-014', 'CHG-2026-014', 'Şema güncelleme')]),
  rec('2026-07-06T09:05:41', 'Mehmet Kaya', 'QA Specialist', 'Onay', 'Approval', 'APR-2026-004', 'apr-2026-004', 'Approve',
    'stage: QA İncelemesi', 'stage: Tamamlandı', 'PROD', 'Success', 'record',
    'QA onay adımı tamamlandı.',
    [
      { field: 'stage', previous: 'QA İncelemesi', current: 'Tamamlandı', changeType: 'modified' },
      { field: 'decision', previous: '—', current: 'Onaylandı', changeType: 'added' },
      { field: 'comment', previous: '—', current: 'Testler başarılı.', changeType: 'added' }
    ],
    [chgRef('chg-2026-017', 'CHG-2026-017', 'API sürüm yükseltme')]),
  rec('2026-07-06T08:47:03', 'Ali Vural', 'Executor', 'Yürütme', 'Execution', 'EXE-2026-006', 'exe-2026-006', 'Execute',
    'progress: 0%', 'progress: 100%', 'PROD', 'Success', 'record',
    'Güvenlik yaması yürütmesi tamamlandı.',
    [
      { field: 'progress', previous: '0%', current: '100%', changeType: 'modified' },
      { field: 'status', previous: 'Ready', current: 'Completed', changeType: 'modified' }
    ],
    [relRef(REL_006, 'REL-2026-006', 'Güvenlik Yaması'), docRef('doc-2026-007', 'DOC-2026-007', 'Yürütme Raporu')]),
  rec('2026-07-06T08:30:22', 'Furkan Demir', 'Architect', 'Kimlik', 'Session', 'furkan.demir', 'sess-8841', 'Login',
    '—', 'session: aktif', 'PROD', 'Success', 'user',
    'Kullanıcı sisteme giriş yaptı.', [], []),
  rec('2026-07-06T02:11:59', 'unknown', 'Anonim', 'Kimlik', 'Session', 'admin', 'sess-fail-2', 'Login',
    '—', 'session: reddedildi', 'PROD', 'Failed', 'security',
    'Geçersiz kimlik bilgileriyle başarısız giriş denemesi.',
    [{ field: 'attempts', previous: '2', current: '3', changeType: 'modified' }], []),
  rec('2026-07-05T22:40:10', 'Ali Vural', 'Executor', 'Yürütme', 'Execution', 'EXE-2026-005', 'exe-2026-005', 'Rollback',
    'state: Executing', 'state: RolledBack', 'PROD', 'Warning', 'record',
    'Üretim yürütmesi geri alındı.',
    [
      { field: 'state', previous: 'Executing', current: 'RolledBack', changeType: 'modified' },
      { field: 'reason', previous: '—', current: 'Doğrulama hatası', changeType: 'added' }
    ],
    [relRef('a0000000-0000-0000-0000-000000000005', 'REL-2026-005', 'EBR v1.2')]),
  rec('2026-07-05T18:20:33', 'System Administrator', 'Administrator', 'Yönetim', 'Customer', 'Nobel İlaç', 'c-nobel', 'Create',
    '—', 'CUST-003 · Nobel İlaç', 'PROD', 'Success', 'compliance',
    'Yeni müşteri tanımı oluşturuldu.',
    [
      { field: 'code', previous: '—', current: 'CUST-003', changeType: 'added' },
      { field: 'name', previous: '—', current: 'Nobel İlaç', changeType: 'added' },
      { field: 'status', previous: '—', current: 'Active', changeType: 'added' }
    ], []),
  rec('2026-07-05T17:10:48', 'Ayşe Yılmaz', 'Architect', 'Doküman', 'Document', 'DOC-2026-002', 'doc-2026-002', 'Update',
    'version: v2', 'version: v3', 'PROD', 'Success', 'record',
    'Şema güncelleme betiği yeni sürümle güncellendi.',
    [
      { field: 'version', previous: 'v2', current: 'v3', changeType: 'modified' },
      { field: 'size', previous: '6 KB', current: '8 KB', changeType: 'modified' }
    ],
    [docRef('doc-2026-002', 'DOC-2026-002', 'Şema Güncelleme Betiği'), relRef(REL_001, 'REL-2026-001', 'EBR Migration v1')]),
  rec('2026-07-05T14:02:15', 'Zeynep Şahin', 'Requester', 'Kimlik', 'User', 'zeynep.sahin', 'usr-04', 'Update',
    'status: Active', 'status: Inactive', 'PROD', 'Success', 'security',
    'Kullanıcı hesabı pasifleştirildi.',
    [{ field: 'status', previous: 'Active', current: 'Inactive', changeType: 'modified' }], []),
  rec('2026-07-05T11:48:07', 'Mehmet Kaya', 'QA Specialist', 'Doğrulama', 'Validation', 'VAL-2026-003', 'val-2026-003', 'Validate',
    'result: —', 'result: Passed', 'UAT', 'Success', 'record',
    'Doğrulama kuralları başarıyla çalıştırıldı.',
    [
      { field: 'result', previous: '—', current: 'Passed', changeType: 'added' },
      { field: 'findings', previous: '—', current: '0 açık bulgu', changeType: 'added' }
    ], []),
  rec('2026-07-05T10:15:52', 'Furkan Demir', 'Architect', 'Varlık', 'Asset', 'AST-2026-004', 'ast-2026-004', 'Update',
    'criticality: High', 'criticality: Critical', 'PROD', 'Success', 'record',
    'EBR Batch API kritiklik seviyesi yükseltildi.',
    [{ field: 'criticality', previous: 'High', current: 'Critical', changeType: 'modified' }],
    [chgRef('chg-2026-017', 'CHG-2026-017', 'API sürüm yükseltme')]),
  rec('2026-07-04T16:30:19', 'System Administrator', 'Administrator', 'Yönetim', 'User', 'elif.aydin', 'usr-06', 'Export',
    '—', 'kullanıcı listesi (CSV)', 'PROD', 'Success', 'compliance',
    'Kullanıcı listesi dışa aktarıldı.', [], []),
  rec('2026-07-04T13:22:41', 'Ali Vural', 'Executor', 'Değişiklik', 'Change', 'CHG-2026-019', 'chg-2026-019', 'Reject',
    'status: Approval', 'status: Rejected', 'PROD', 'Failed', 'record',
    'Güvenlik yaması değişikliği reddedildi.',
    [
      { field: 'status', previous: 'Approval', current: 'Rejected', changeType: 'modified' },
      { field: 'reason', previous: '—', current: 'Ek test gerekiyor', changeType: 'added' }
    ], []),
  rec('2026-07-04T09:00:05', 'Elif Aydın', 'Viewer', 'Kimlik', 'Session', 'elif.aydin', 'sess-7712', 'Logout',
    'session: aktif', '—', 'TEST', 'Success', 'user',
    'Kullanıcı oturumu kapattı.', [], []),
  rec('2026-07-03T15:44:38', 'System Administrator', 'Administrator', 'Yönetim', 'User', 'new.user', 'usr-99', 'Create',
    '—', 'yeni kullanıcı hesabı', 'PROD', 'Warning', 'security',
    'Yeni kullanıcı hesabı oluşturuldu; rol ataması bekliyor.',
    [
      { field: 'email', previous: '—', current: 'new.user@gms.local', changeType: 'added' },
      { field: 'role', previous: '—', current: 'Atanmadı', changeType: 'added' }
    ], [])
];

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly store = signal<AuditRecord[]>(SEED);

  getRecords(): Observable<AuditRecord[]> {
    return of(this.store());
  }
  getRecord(id: string): Observable<AuditRecord | undefined> {
    return of(this.store().find((r) => r.id === id));
  }
}
