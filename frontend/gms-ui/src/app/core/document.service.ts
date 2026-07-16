import { Injectable, signal } from '@angular/core';
import { Observable, of } from 'rxjs';
import { IconName } from '../shared/icon/icon';

export type DocPreviewKind = 'pdf' | 'sql' | 'text' | 'image' | 'word' | 'excel';

/**
 * Document type catalog — reusable across every governance object. New types are
 * added here; the UI (filters, icons, upload) reads from it. No storage engine.
 */
export interface DocumentType {
  key: string;
  label: string;
  icon: IconName;
}

export const DOCUMENT_TYPES: DocumentType[] = [
  { key: 'sql-script', label: 'SQL Betiği', icon: 'server' },
  { key: 'rollback-script', label: 'Geri Alma Betiği', icon: 'change' },
  { key: 'validation-report', label: 'Doğrulama Raporu', icon: 'shield' },
  { key: 'execution-report', label: 'Yürütme Raporu', icon: 'execution' },
  { key: 'release-note', label: 'Sürüm Notu', icon: 'release' },
  { key: 'test-evidence', label: 'Test Kanıtı', icon: 'check' },
  { key: 'approval-evidence', label: 'Onay Kanıtı', icon: 'approval' },
  { key: 'architecture-doc', label: 'Mimari Doküman', icon: 'orgchart' },
  { key: 'operational-procedure', label: 'Operasyonel Prosedür', icon: 'document' },
  { key: 'other', label: 'Diğer', icon: 'document' }
];

export function docTypeLabel(key: string): string {
  return DOCUMENT_TYPES.find((t) => t.key === key)?.label ?? key;
}
export function docTypeIcon(key: string): IconName {
  return DOCUMENT_TYPES.find((t) => t.key === key)?.icon ?? 'document';
}

export const DOCUMENT_STATUSES = ['Draft', 'Active', 'Archived', 'Expired'];

export interface DocVersion {
  version: string;
  createdBy: string;
  createdAt: string;
  description: string;
  status: string;
}

export interface GmsDocument {
  id: string;
  code: string;
  name: string;
  category: string; // document type key
  fileName: string;
  preview: DocPreviewKind;
  size: string;
  releaseId: string | null;
  releaseCode: string | null;
  changeId: string | null;
  changeCode: string | null;
  projectName: string;
  customerName: string;
  owner: string;
  version: string;
  status: string;
  description: string;
  createdAt: string;
  updatedAt: string;
  versions: DocVersion[];
}

export interface UploadDocumentPayload {
  name: string;
  category: string;
  fileName: string;
  preview: DocPreviewKind;
  size: string;
  releaseId: string | null;
  releaseCode: string | null;
  projectName: string;
  description: string;
  owner: string;
}

const KEY = 'gms.documents';

function versions(current: string, owner: string, created: string, updated: string): DocVersion[] {
  const list: DocVersion[] = [];
  const n = parseInt(current.replace(/[^0-9]/g, ''), 10) || 1;
  for (let v = n; v >= 1; v--) {
    list.push({
      version: `v${v}`,
      createdBy: v === n ? owner : 'Ayşe Yılmaz',
      createdAt: v === n ? updated : created,
      description: v === n ? 'Güncel sürüm.' : v === 1 ? 'İlk sürüm oluşturuldu.' : 'Ara revizyon.',
      status: v === n ? 'Active' : 'Archived'
    });
  }
  return list;
}

function doc(
  code: string, name: string, category: string, fileName: string, preview: DocPreviewKind, size: string,
  releaseId: string | null, releaseCode: string | null, changeId: string | null, changeCode: string | null,
  projectName: string, customerName: string, owner: string, version: string, status: string,
  description: string, createdAt: string, updatedAt: string
): GmsDocument {
  return {
    id: code.toLowerCase(), code, name, category, fileName, preview, size,
    releaseId, releaseCode, changeId, changeCode, projectName, customerName, owner,
    version, status, description, createdAt, updatedAt,
    versions: versions(version, owner, createdAt, updatedAt)
  };
}

const SEED: GmsDocument[] = [
  doc('DOC-2026-001', 'Validasyon Planı', 'validation-report', 'validasyon-plani.pdf', 'pdf', '1.2 MB', '07777777-7777-7777-7777-777777777701', 'REL-2026-001', 'chg-2026-014', 'CHG-2026-014', 'EBR Migration', 'Abdi İbrahim', 'Mehmet Kaya', 'v2', 'Active', 'EBR üretim yayını için bilgisayarlı sistem validasyon planı.', '2026-02-10T09:00:00', '2026-03-04T14:00:00'),
  doc('DOC-2026-002', 'Şema Güncelleme Betiği', 'sql-script', 'schema-update.sql', 'sql', '8 KB', '07777777-7777-7777-7777-777777777701', 'REL-2026-001', 'chg-2026-014', 'CHG-2026-014', 'EBR Migration', 'Abdi İbrahim', 'Ayşe Yılmaz', 'v3', 'Active', 'Veritabanı şema güncelleme betiği.', '2026-02-20T10:00:00', '2026-03-05T11:00:00'),
  doc('DOC-2026-003', 'Geri Alma Betiği', 'rollback-script', 'rollback.sql', 'sql', '5 KB', '07777777-7777-7777-7777-777777777701', 'REL-2026-001', 'chg-2026-014', 'CHG-2026-014', 'EBR Migration', 'Abdi İbrahim', 'Ali Vural', 'v1', 'Active', 'Şema değişikliği için geri alma betiği.', '2026-02-21T10:00:00', '2026-02-21T10:00:00'),
  doc('DOC-2026-004', 'Sürüm Notları', 'release-note', 'release-notes.txt', 'text', '3 KB', '07777777-7777-7777-7777-777777777701', 'REL-2026-001', null, null, 'EBR Migration', 'Abdi İbrahim', 'System Administrator', 'v1', 'Draft', 'REL-2026-001 sürüm notları taslağı.', '2026-03-01T08:00:00', '2026-03-01T08:00:00'),
  doc('DOC-2026-005', 'UAT Test Kanıtı', 'test-evidence', 'uat-evidence.png', 'image', '640 KB', 'a0000000-0000-0000-0000-000000000004', 'REL-2026-004', 'chg-2026-017', 'CHG-2026-017', 'MES Upgrade', 'Bilim İlaç', 'Ali Vural', 'v1', 'Active', 'UAT ortamı test ekran görüntüsü kanıtı.', '2026-03-11T10:00:00', '2026-03-11T10:00:00'),
  doc('DOC-2026-006', 'Mimari Tasarım', 'architecture-doc', 'architecture.docx', 'word', '2.1 MB', 'a0000000-0000-0000-0000-000000000003', 'REL-2026-003', null, null, 'EBR Migration', 'Abdi İbrahim', 'Furkan Demir', 'v2', 'Active', 'EBR Migration mimari tasarım dokümanı.', '2026-01-15T09:00:00', '2026-02-28T16:00:00'),
  doc('DOC-2026-007', 'Yürütme Raporu', 'execution-report', 'execution-report.xlsx', 'excel', '420 KB', 'a0000000-0000-0000-0000-000000000006', 'REL-2026-006', 'chg-2026-019', 'CHG-2026-019', 'MES Upgrade', 'Bilim İlaç', 'Ali Vural', 'v1', 'Archived', 'Güvenlik yaması yürütme raporu.', '2026-02-27T18:00:00', '2026-02-28T09:00:00'),
  doc('DOC-2026-008', 'Onay Kanıtı', 'approval-evidence', 'approval.pdf', 'pdf', '260 KB', 'a0000000-0000-0000-0000-000000000006', 'REL-2026-006', 'chg-2026-019', 'CHG-2026-019', 'MES Upgrade', 'Bilim İlaç', 'System Administrator', 'v1', 'Expired', 'Onay adımlarının imzalı kaydı.', '2026-02-25T09:00:00', '2026-02-25T09:00:00')
];

/**
 * Document data service. Frontend-only (no real file storage / cloud) —
 * persisted to localStorage. Upload captures metadata only.
 */
@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly store = signal<GmsDocument[]>(this.load());
  private seq = 100;

  getDocuments(): Observable<GmsDocument[]> {
    return of(this.store());
  }

  getDocument(id: string): Observable<GmsDocument | undefined> {
    return of(this.store().find((d) => d.id === id));
  }

  upload(payload: UploadDocumentPayload): Observable<GmsDocument> {
    const n = String(9 + (this.seq++ % 900)).padStart(3, '0');
    const now = new Date().toISOString();
    const document: GmsDocument = {
      id: 'doc-' + this.seq,
      code: `DOC-2026-${n}`,
      changeId: null,
      changeCode: null,
      customerName: '—',
      version: 'v1',
      status: 'Draft',
      createdAt: now,
      updatedAt: now,
      versions: [{ version: 'v1', createdBy: payload.owner, createdAt: now, description: 'İlk sürüm yüklendi.', status: 'Draft' }],
      ...payload
    };
    this.store.update((list) => [document, ...list]);
    this.persist();
    return of(document);
  }

  private load(): GmsDocument[] {
    try {
      const raw = localStorage.getItem(KEY);
      return raw ? (JSON.parse(raw) as GmsDocument[]) : SEED;
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
