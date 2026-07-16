import { Release, ReleaseRisk } from '../../core/release.service';
import { ActivityItem } from '../../shared/ui/activity-feed/activity-feed';
import { TimelineItem } from '../../shared/ui/timeline/timeline';
import { LinkItem } from '../../shared/ui/item-list/item-list';
import { BadgeTone } from '../../shared/ui/badge/badge';

/** Translate callback shape shared by every VM helper below (bound from TranslocoService.translate). */
export type Translate = (key: string, params?: Record<string, unknown>) => string;

/* ─────────────────────────────────────────────────────────
 * Release risk model — auto-calculated, never chosen manually. Shared by the
 * wizard (live preview) and the detail page. Reuses the root-scope badge.risk.*
 * translations (already bilingual) instead of a local dictionary.
 * ───────────────────────────────────────────────────────── */

export const RELEASE_RISK_META: Record<ReleaseRisk, { labelKey: string; tone: BadgeTone }> = {
  Critical: { labelKey: 'badge.risk.Critical', tone: 'danger' },
  High: { labelKey: 'badge.risk.High', tone: 'danger' },
  Medium: { labelKey: 'badge.risk.Medium', tone: 'warning' },
  Low: { labelKey: 'badge.risk.Low', tone: 'success' }
};

const REL_ENV_WEIGHT: Record<string, number> = { PROD: 3, PREPROD: 2, UAT: 2, TEST: 1, DEV: 0 };

export function calcReleaseRisk(environment: string, changeCount: number, emergencyCount: number, criticalAssetCount: number): ReleaseRisk {
  const env = REL_ENV_WEIGHT[environment] ?? 0;
  const cnt = changeCount >= 8 ? 3 : changeCount >= 5 ? 2 : changeCount >= 3 ? 1 : 0;
  const emg = emergencyCount >= 2 ? 3 : emergencyCount >= 1 ? 2 : 0;
  const crit = criticalAssetCount >= 3 ? 3 : criticalAssetCount >= 1 ? 1 : 0;
  const score = env + cnt + emg + crit;
  if (score >= 8) return 'Critical';
  if (score >= 5.5) return 'High';
  if (score >= 3) return 'Medium';
  return 'Low';
}

/** Estimated implementation minutes per change type — feeds duration rollups. */
const TYPE_MINUTES: Record<string, number> = {
  'app-deploy': 30, 'db-schema': 45, 'sql-fix': 20, 'sp-func': 25, 'api': 25,
  'config': 15, 'infra': 60, 'integration': 40, 'doc-sop': 10, 'other': 20
};
export function estMinutes(changeType: string): number {
  return TYPE_MINUTES[changeType] ?? 20;
}
export function formatDuration(mins: number, t: Translate): string {
  if (mins <= 0) return t('releases.duration.zero');
  const h = Math.floor(mins / 60);
  const m = mins % 60;
  if (h && m) return t('releases.duration.hm', { h, m });
  if (h) return t('releases.duration.h', { h });
  return t('releases.duration.m', { m });
}

/** Release type catalog (major/minor/patch/hotfix/emergency) — the core RELEASE_TYPES
 * array carries hardcoded Turkish labels, so the UI resolves display text from this
 * local key instead of core's `label` field. */
export function releaseTypeKey(key: string): string {
  return 'releases.type.' + key;
}

/**
 * Release view-model layer.
 *
 * The backend ReleaseDto currently exposes: id, name, version, projectName,
 * environmentName, plannedDate, status, createdAt, createdByUserName.
 * Fields the DTO does not yet carry (customer, progress, risk, related changes,
 * last-updated) are DERIVED here — deterministically from the release — so the UI
 * is complete today and the backend can populate them later with zero UI changes.
 */

export interface ReleaseRow {
  id: string;
  code: string;
  version: string;
  projectName: string;
  customerName: string;
  environmentName: string;
  owner: string;
  plannedDate: string | null;
  status: string;
  progress: number;
  changes: number;
  risk: ReleaseRisk;
  lastUpdated: string;
  raw: Release;
}

/** Status → progress %. Backend statuses + Release lifecycle vocabulary. */
export const STATUS_PROGRESS: Record<string, number> = {
  Draft: 5,
  Planning: 20,
  Planned: 25,
  Validation: 45,
  InReview: 45,
  Approval: 65,
  Approved: 75,
  Ready: 80,
  Executing: 92,
  Executed: 100,
  Completed: 100,
  Cancelled: 0
};

/** Release status filter options (governance lifecycle). */
export const RELEASE_STATUSES: string[] = [
  'Draft',
  'Planning',
  'Validation',
  'Approval',
  'Ready',
  'Executing',
  'Completed',
  'Cancelled'
];

function hash(input: string): number {
  let h = 0;
  for (let i = 0; i < input.length; i++) {
    h = (h * 31 + input.charCodeAt(i)) & 0xffffffff;
  }
  return Math.abs(h);
}

function deriveRisk(id: string): ReleaseRisk {
  const r = hash(id) % 3;
  return r === 0 ? 'High' : r === 1 ? 'Medium' : 'Low';
}

function deriveChanges(id: string): number {
  return hash(id + 'chg') % 6;
}

/** Locale to use for `Date#toLocaleDateString` given the active app language. */
export function dateLocale(lang: string): string {
  return lang === 'en' ? 'en-US' : 'tr-TR';
}

export function relativeTime(iso: string, t: Translate, locale: string): string {
  const then = new Date(iso).getTime();
  if (isNaN(then)) return '—';
  const diff = Date.now() - then;
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return t('releases.time.justNow');
  if (mins < 60) return t('releases.time.minutesAgo', { mins });
  const hours = Math.floor(mins / 60);
  if (hours < 24) return t('releases.time.hoursAgo', { hours });
  const days = Math.floor(hours / 24);
  if (days < 30) return t('releases.time.daysAgo', { days });
  return new Date(iso).toLocaleDateString(locale);
}

export function toRow(r: Release, customerName: string, t: Translate, locale: string): ReleaseRow {
  // Prefer real wizard data; fall back to deterministic derivation for legacy seed.
  const changeCount = r.changes ? r.changes.length : deriveChanges(r.id);
  return {
    id: r.id,
    code: r.name,
    version: r.version,
    projectName: r.projectName,
    customerName: r.customerName ?? customerName,
    environmentName: r.environmentName,
    owner: r.releaseManager ?? r.createdByUserName,
    plannedDate: r.plannedDate,
    status: r.status,
    progress: STATUS_PROGRESS[r.status] ?? 10,
    changes: changeCount,
    risk: r.risk ?? deriveRisk(r.id),
    lastUpdated: relativeTime(r.createdAt, t, locale),
    raw: r
  };
}

/* ---------- Release Detail mock data (reusable, architecture-first) ---------- */

export function detailTimeline(row: ReleaseRow, t: Translate, locale: string): TimelineItem[] {
  return [
    { title: t('releases.detail.timeline.created'), time: new Date('2026-01-01').toLocaleDateString(locale), tone: 'info', icon: 'release', description: t('releases.detail.timeline.createdDesc', { owner: row.owner }) },
    { title: t('releases.detail.timeline.updated'), time: new Date('2026-01-02').toLocaleDateString(locale), tone: 'info', icon: 'change' },
    { title: t('releases.detail.timeline.validationStarted'), time: new Date('2026-01-03').toLocaleDateString(locale), tone: 'warning', icon: 'shield', description: t('releases.detail.timeline.validationStartedDesc') },
    { title: t('releases.detail.timeline.approvalPending'), time: t('releases.detail.timeline.pending'), tone: 'warning', icon: 'approval' },
    { title: t('releases.detail.timeline.executionScheduled'), time: row.plannedDate ? new Date(row.plannedDate).toLocaleDateString(locale) : t('releases.detail.timeline.notScheduled'), tone: 'neutral', icon: 'execution' }
  ];
}

export function detailActivity(row: ReleaseRow, t: Translate): ActivityItem[] {
  return [
    { actor: row.owner, action: t('releases.detail.activity.createdRelease'), time: t('releases.time.daysAgo', { days: 2 }), icon: 'release' },
    { actor: 'Ayşe Yılmaz', action: t('releases.detail.activity.addedValidationPlan'), time: t('releases.time.daysAgo', { days: 1 }), icon: 'shield' },
    { actor: 'Mehmet Kaya', action: t('releases.detail.activity.leftComment'), time: t('releases.time.hoursAgo', { hours: 5 }), icon: 'document' }
  ];
}

export function detailDocuments(): LinkItem[] {
  return [
    { id: 'd1', label: 'Validasyon Planı v2.pdf', hint: 'PDF · 1.2 MB', route: '/releases', icon: 'document' },
    { id: 'd2', label: 'Risk Değerlendirme.xlsx', hint: 'XLSX · 340 KB', route: '/releases', icon: 'document' },
    { id: 'd3', label: 'Yayın Notları.docx', hint: 'DOCX · 88 KB', route: '/releases', icon: 'document' }
  ];
}

export interface CommentItem {
  author: string;
  text: string;
  time: string;
}

export function detailComments(t: Translate): CommentItem[] {
  return [
    { author: 'Ayşe Yılmaz', text: 'UAT ortamında doğrulama tamamlandı, PROD için hazır.', time: t('releases.time.hoursAgo', { hours: 3 }) },
    { author: 'Mehmet Kaya', text: 'Risk değerlendirmesini güncelledim.', time: t('releases.time.yesterday') }
  ];
}

export function detailUpcoming(t: Translate): LinkItem[] {
  return [
    { id: 'u1', label: t('releases.detail.upcoming.approvalMeeting'), hint: t('releases.detail.upcoming.tomorrowAt', { time: '10:00' }), route: '/releases', icon: 'approval' },
    { id: 'u2', label: t('releases.detail.upcoming.validationDeadline'), hint: t('releases.detail.upcoming.inDays', { days: 3 }), route: '/releases', icon: 'clock' }
  ];
}

/* ---------- Changes tab (architecture only) ---------- */

export interface ChangeRow {
  id: string;
  title: string;
  type: string;
  risk: 'High' | 'Medium' | 'Low';
  status: string;
  createdBy: string;
  lastUpdated: string;
}

export function detailChanges(row: ReleaseRow, t: Translate): ChangeRow[] {
  // Real included changes from the wizard model when available.
  const included = row.raw.changes;
  if (included && included.length) {
    return included.map((c) => ({
      id: c.code,
      title: c.title,
      type: c.changeType,
      risk: (c.risk === 'Critical' ? 'High' : c.risk) as 'High' | 'Medium' | 'Low',
      status: 'Approved',
      createdBy: c.owner,
      lastUpdated: '—'
    }));
  }
  if (row.changes === 0) return [];
  const base: ChangeRow[] = [
    { id: 'CHG-2026-014', title: 'Veritabanı şeması güncellemesi', type: 'Standart', risk: 'Medium', status: 'InReview', createdBy: row.owner, lastUpdated: t('releases.time.hoursAgo', { hours: 2 }) },
    { id: 'CHG-2026-015', title: 'Yetki matrisi revizyonu', type: 'Acil', risk: 'High', status: 'Approval', createdBy: 'Ayşe Yılmaz', lastUpdated: t('releases.time.yesterday') },
    { id: 'CHG-2026-016', title: 'Rapor şablonu ekleme', type: 'Standart', risk: 'Low', status: 'Draft', createdBy: 'Mehmet Kaya', lastUpdated: t('releases.time.daysAgo', { days: 3 }) }
  ];
  return base.slice(0, Math.min(row.changes, base.length));
}
