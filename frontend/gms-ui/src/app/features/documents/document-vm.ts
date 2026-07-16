import { TranslocoService } from '@jsverse/transloco';
import { GmsDocument } from '../../core/document.service';
import { ActivityItem } from '../../shared/ui/activity-feed/activity-feed';
import { TimelineItem } from '../../shared/ui/timeline/timeline';
import { LinkItem } from '../../shared/ui/item-list/item-list';
import { relativeTime } from '../releases/release-vm';

export { relativeTime };

/**
 * Document type catalog labels live in core/document.service.ts (DOCUMENT_TYPES) and are
 * hardcoded Turkish there. Since that core service is shared/out of scope, we keep our own
 * translated dictionary here (keyed by the same `category` key) independent of the core labels.
 */
const DOCUMENT_TYPE_KEYS: Record<string, string> = {
  'sql-script': 'documents.type.sqlScript',
  'rollback-script': 'documents.type.rollbackScript',
  'validation-report': 'documents.type.validationReport',
  'execution-report': 'documents.type.executionReport',
  'release-note': 'documents.type.releaseNote',
  'test-evidence': 'documents.type.testEvidence',
  'approval-evidence': 'documents.type.approvalEvidence',
  'architecture-doc': 'documents.type.architectureDoc',
  'operational-procedure': 'documents.type.operationalProcedure',
  other: 'documents.type.other'
};

export function documentTypeLabel(key: string, t: TranslocoService): string {
  const labelKey = DOCUMENT_TYPE_KEYS[key];
  return labelKey ? t.translate(labelKey) : key;
}

export function detailTimeline(d: GmsDocument, t: TranslocoService): TimelineItem[] {
  const c = new Date(d.createdAt).toLocaleDateString('tr-TR');
  const u = new Date(d.updatedAt).toLocaleDateString('tr-TR');
  return [
    { title: t.translate('documents.detail.timeline.created'), time: c, tone: 'info', icon: 'document', description: t.translate('documents.detail.timeline.createdBy', { owner: d.owner }) },
    { title: t.translate('documents.detail.timeline.fileUploaded'), time: c, tone: 'info', icon: 'document' },
    { title: t.translate('documents.detail.timeline.updated'), time: u, tone: 'info', icon: 'change' },
    { title: t.translate('documents.detail.timeline.versionPublished'), time: `${d.version} · ${u}`, tone: 'success', icon: 'check' },
    { title: t.translate('documents.detail.timeline.archived'), time: d.status === 'Archived' ? u : t.translate('documents.detail.timeline.pending'), tone: 'neutral', icon: 'folder' }
  ];
}

export function detailActivity(d: GmsDocument, t: TranslocoService): ActivityItem[] {
  return [
    { actor: d.owner, action: t.translate('documents.detail.activity.publishedVersion', { version: d.version }), time: t.translate('documents.detail.activity.twoDaysAgo'), icon: 'document' },
    { actor: 'Ayşe Yılmaz', action: t.translate('documents.detail.activity.viewed'), time: t.translate('documents.detail.activity.oneDayAgo'), icon: 'search' },
    { actor: 'Mehmet Kaya', action: t.translate('documents.detail.activity.commented'), time: t.translate('documents.detail.activity.fiveHoursAgo'), icon: 'inbox' }
  ];
}

export function relatedLinks(d: GmsDocument, t: TranslocoService): LinkItem[] {
  const items: LinkItem[] = [];
  if (d.releaseId && d.releaseCode) items.push({ id: d.releaseId, label: d.releaseCode, hint: t.translate('documents.detail.relatedRelease'), route: `/releases/${d.releaseId}`, icon: 'release' });
  if (d.changeId && d.changeCode) items.push({ id: d.changeId, label: d.changeCode, hint: t.translate('documents.detail.relatedChange'), route: `/changes/${d.changeId}`, icon: 'change' });
  return items;
}

export function recentDocuments(d: GmsDocument): LinkItem[] {
  const items: LinkItem[] = [
    { id: 'doc-2026-001', label: 'Validasyon Planı', hint: 'PDF · v2', route: '/documents/doc-2026-001', icon: 'document' },
    { id: 'doc-2026-002', label: 'Şema Güncelleme Betiği', hint: 'SQL · v3', route: '/documents/doc-2026-002', icon: 'document' },
    { id: 'doc-2026-006', label: 'Mimari Tasarım', hint: 'Word · v2', route: '/documents/doc-2026-006', icon: 'document' }
  ];
  return items.filter((x) => x.id !== d.id);
}

/* ---------- Mock preview content ---------- */

export function previewCode(d: GmsDocument): string {
  return [
    `-- ${d.name} (${d.version})`,
    'BEGIN TRANSACTION;',
    '',
    'ALTER TABLE batch_record',
    '  ADD reviewed_by NVARCHAR(100) NULL;',
    '',
    "UPDATE batch_record",
    "  SET status = 'REVIEWED'",
    "  WHERE status = 'PENDING';",
    '',
    'COMMIT TRANSACTION;'
  ].join('\n');
}

export function previewText(d: GmsDocument, t: TranslocoService): string[] {
  return [
    d.name,
    '',
    d.description,
    '',
    `${t.translate('documents.detail.field.version')}: ${d.version}`,
    `${t.translate('documents.detail.field.owner')}: ${d.owner}`,
    `${t.translate('documents.detail.field.category')}: ${d.category}`,
    '',
    t.translate('documents.detail.previewNote1'),
    t.translate('documents.detail.previewNote2')
  ];
}
