import { TranslocoService } from '@jsverse/transloco';
import { AuditRecord, AuditAction, AuditResult, FieldChangeType, actionMeta, resultMeta } from '../../core/audit.service';
import { AuditEntry } from '../../shared/ui/audit-list/audit-list';
import { TimelineItem } from '../../shared/ui/timeline/timeline';
import { LinkItem } from '../../shared/ui/item-list/item-list';
import { ActivityItem } from '../../shared/ui/activity-feed/activity-feed';
import { BadgeTone } from '../../shared/ui/badge/badge';
import { IconName } from '../../shared/icon/icon';

/**
 * core/audit.service.ts (ACTION_META / RESULT_META / CHANGE_TYPE_META / AUDIT_MODULES) holds
 * hardcoded Turkish labels and is out of scope to edit. We keep our own translated label
 * dictionaries here, reusing only the tone/icon metadata from the core service.
 */
const ACTION_LABEL_KEYS: Record<AuditAction, string> = {
  Create: 'audit.action.create',
  Update: 'audit.action.update',
  Delete: 'audit.action.delete',
  Approve: 'audit.action.approve',
  Reject: 'audit.action.reject',
  Validate: 'audit.action.validate',
  Execute: 'audit.action.execute',
  Rollback: 'audit.action.rollback',
  Login: 'audit.action.login',
  Logout: 'audit.action.logout',
  Export: 'audit.action.export',
  Import: 'audit.action.import'
};

export interface LocalMeta {
  label: string;
  tone: BadgeTone;
  icon: IconName;
}

export function localActionMeta(action: string, t: TranslocoService): LocalMeta {
  const core = actionMeta(action);
  const key = ACTION_LABEL_KEYS[action as AuditAction];
  return { label: key ? t.translate(key) : core.label, tone: core.tone, icon: core.icon };
}

const RESULT_LABEL_KEYS: Record<AuditResult, string> = {
  Success: 'audit.result.success',
  Failed: 'audit.result.failed',
  Warning: 'audit.result.warning'
};

export function localResultMeta(result: string, t: TranslocoService): { label: string; tone: BadgeTone } {
  const core = resultMeta(result);
  const key = RESULT_LABEL_KEYS[result as AuditResult];
  return { label: key ? t.translate(key) : core.label, tone: core.tone };
}

const CHANGE_TYPE_LABEL_KEYS: Record<FieldChangeType, string> = {
  added: 'audit.changeType.added',
  modified: 'audit.changeType.modified',
  removed: 'audit.changeType.removed',
  unchanged: 'audit.changeType.unchanged'
};
const CHANGE_TYPE_TONE: Record<FieldChangeType, BadgeTone> = {
  added: 'success',
  modified: 'warning',
  removed: 'danger',
  unchanged: 'neutral'
};

export function localChangeTypeMeta(changeType: string, t: TranslocoService): { label: string; tone: BadgeTone } {
  const key = CHANGE_TYPE_LABEL_KEYS[changeType as FieldChangeType];
  const tone = CHANGE_TYPE_TONE[changeType as FieldChangeType] ?? 'neutral';
  return { label: key ? t.translate(key) : changeType, tone };
}

/** AUDIT_MODULES in core/audit.service.ts are literal Turkish strings (used as record values too). */
const MODULE_LABEL_KEYS: Record<string, string> = {
  'Yayın': 'audit.module.release',
  'Değişiklik': 'audit.module.change',
  'Onay': 'audit.module.approval',
  'Yürütme': 'audit.module.execution',
  'Doğrulama': 'audit.module.validation',
  'Doküman': 'audit.module.document',
  'Varlık': 'audit.module.asset',
  'Yönetim': 'audit.module.administration',
  'Kimlik': 'audit.module.identity'
};

export function moduleLabel(module: string, t: TranslocoService): string {
  const key = MODULE_LABEL_KEYS[module];
  return key ? t.translate(key) : module;
}

export function relTime(iso: string, t: TranslocoService): string {
  const diff = Date.now() - new Date(iso).getTime();
  const m = Math.floor(diff / 60000);
  if (m < 1) return t.translate('audit.time.justNow');
  if (m < 60) return t.translate('audit.time.minutesAgo', { m });
  const h = Math.floor(m / 60);
  if (h < 24) return t.translate('audit.time.hoursAgo', { h });
  const d = Math.floor(h / 24);
  return t.translate('audit.time.daysAgo', { d });
}

/** Map an audit record to the reusable audit-list entry shape. */
export function toEntry(r: AuditRecord, t: TranslocoService): AuditEntry {
  const am = localActionMeta(r.action, t);
  return {
    user: r.user,
    action: `${am.label} · ${r.objectName}`,
    description: r.description,
    time: relTime(r.timestamp, t),
    status: localResultMeta(r.result, t)
  };
}

export function recentActivities(records: AuditRecord[], t: TranslocoService): AuditEntry[] {
  return [...records].sort((a, b) => b.timestamp.localeCompare(a.timestamp)).slice(0, 6).map((r) => toEntry(r, t));
}

export function criticalEvents(records: AuditRecord[], t: TranslocoService): AuditEntry[] {
  return records
    .filter((r) => r.result === 'Failed' || r.action === 'Delete' || r.action === 'Rollback' || r.action === 'Reject')
    .sort((a, b) => b.timestamp.localeCompare(a.timestamp))
    .slice(0, 5)
    .map((r) => toEntry(r, t));
}

export function recentSecurity(records: AuditRecord[], t: TranslocoService): AuditEntry[] {
  return records.filter((r) => r.category === 'security').sort((a, b) => b.timestamp.localeCompare(a.timestamp)).slice(0, 5).map((r) => toEntry(r, t));
}

/** Aggregate: most active users (by record count). */
export function mostActiveUsers(records: AuditRecord[], t: TranslocoService): LinkItem[] {
  const map = new Map<string, number>();
  for (const r of records) map.set(r.user, (map.get(r.user) ?? 0) + 1);
  return [...map.entries()]
    .sort((a, b) => b[1] - a[1])
    .slice(0, 5)
    .map(([user, n]): LinkItem => ({ id: user, label: user, hint: t.translate('audit.operationsCount', { n }), route: '/audit', icon: 'user' }));
}

/** Aggregate: most modified objects (by record count). */
export function mostModifiedObjects(records: AuditRecord[], t: TranslocoService): LinkItem[] {
  const map = new Map<string, { count: number; module: string }>();
  for (const r of records) {
    const cur = map.get(r.objectName) ?? { count: 0, module: r.module };
    cur.count++;
    map.set(r.objectName, cur);
  }
  return [...map.entries()]
    .sort((a, b) => b[1].count - a[1].count)
    .slice(0, 5)
    .map(([name, v]): LinkItem => ({ id: name, label: name, hint: `${moduleLabel(v.module, t)} · ${t.translate('audit.changesCount', { n: v.count })}`, route: '/audit', icon: 'folder' }));
}

/* ---------- Detail ---------- */

export function detailTimeline(r: AuditRecord, t: TranslocoService): TimelineItem[] {
  const am = localActionMeta(r.action, t);
  const rm = localResultMeta(r.result, t);
  const time = new Date(r.timestamp).toLocaleString('tr-TR');
  return [
    { title: t.translate('audit.detail.timeline.recordCreated'), time, tone: 'info', icon: 'plus', description: t.translate('audit.detail.timeline.recordCreatedBy', { user: r.user }) },
    { title: t.translate('audit.detail.timeline.actionTitle', { action: am.label }), time, tone: am.tone, icon: am.icon, description: r.description },
    { title: t.translate('audit.detail.timeline.result'), time, tone: rm.tone, icon: 'audit', description: rm.label },
    { title: t.translate('audit.detail.timeline.immutableRecord'), time: t.translate('audit.detail.timeline.permanent'), tone: 'neutral', icon: 'lock', description: t.translate('audit.detail.timeline.immutableRecordDescription') }
  ];
}

export function relatedLinks(r: AuditRecord): LinkItem[] {
  return r.related.map((x): LinkItem => ({
    id: x.id,
    label: x.code,
    hint: x.name,
    route: x.route,
    icon: x.kind === 'release' ? 'release' : x.kind === 'change' ? 'change' : 'document'
  }));
}

export function relatedByKind(r: AuditRecord, kind: 'release' | 'change' | 'document'): LinkItem[] {
  return relatedLinks(r).filter((_, i) => r.related[i].kind === kind);
}

export function recentActivityFeed(records: AuditRecord[], excludeId: string, t: TranslocoService): ActivityItem[] {
  return records
    .filter((r) => r.id !== excludeId)
    .sort((a, b) => b.timestamp.localeCompare(a.timestamp))
    .slice(0, 6)
    .map((r): ActivityItem => {
      const am = localActionMeta(r.action, t);
      return { actor: r.user, action: `${am.label} · ${r.objectName}`, time: relTime(r.timestamp, t), icon: am.icon };
    });
}
