import { Change, RiskLevel, ChangeClass, AffectedAssetRef } from '../../core/change.service';
import { ActivityItem } from '../../shared/ui/activity-feed/activity-feed';
import { TimelineItem } from '../../shared/ui/timeline/timeline';
import { LinkItem } from '../../shared/ui/item-list/item-list';
import { BadgeTone } from '../../shared/ui/badge/badge';

/** Translate callback shape shared by every VM helper below (bound from TranslocoService.translate). */
export type Translate = (key: string, params?: Record<string, unknown>) => string;

/** Local relative-time formatter for the `changes` scope (kept independent of the
 * `releases` scope so this feature never depends on another feature's i18n scope
 * being loaded). */
export function relativeTime(iso: string, t: Translate, locale: string): string {
  const then = new Date(iso).getTime();
  if (isNaN(then)) return '—';
  const diff = Date.now() - then;
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return t('changes.time.justNow');
  if (mins < 60) return t('changes.time.minutesAgo', { mins });
  const hours = Math.floor(mins / 60);
  if (hours < 24) return t('changes.time.hoursAgo', { hours });
  const days = Math.floor(hours / 24);
  if (days < 30) return t('changes.time.daysAgo', { days });
  return new Date(iso).toLocaleDateString(locale);
}

/** Locale to use for `Date#toLocaleDateString` given the active app language. */
export function dateLocale(lang: string): string {
  return lang === 'en' ? 'en-US' : 'tr-TR';
}

/** Change class / type catalogs — core's CHANGE_CLASSES / CHANGE_TYPES carry hardcoded
 * Turkish labels, so the UI resolves display text from these local keys instead. */
export function changeClassKey(key: string): string {
  return 'changes.class.' + key;
}
export function changeTypeKey(key: string): string {
  return 'changes.type.' + key;
}
export function docTypeKey(key: string): string {
  return 'changes.docType.' + key;
}
export function assetTypeKey(key: string): string {
  return 'changes.assetType.' + key;
}

/* ─────────────────────────────────────────────────────────
 * Risk model — auto-calculated, never chosen manually. Shared by the wizard
 * (live preview) and any module that needs to re-derive a change's risk.
 * ───────────────────────────────────────────────────────── */

const ENV_WEIGHT: Record<string, number> = { PROD: 3, PREPROD: 2, UAT: 2, TEST: 1, DEV: 0 };
const CLASS_WEIGHT: Record<ChangeClass, number> = { Emergency: 3, Normal: 1, Standard: 0 };
const TYPE_WEIGHT: Record<string, number> = {
  'db-schema': 2, 'sql-fix': 2, 'infra': 2, 'api': 2,
  'sp-func': 1, 'config': 1, 'integration': 1,
  'app-deploy': 1, 'doc-sop': 0, 'other': 0
};

export function calculateRisk(environment: string, changeClass: ChangeClass, changeType: string, assets: AffectedAssetRef[]): RiskLevel {
  const env = ENV_WEIGHT[environment] ?? 0;
  const cls = CLASS_WEIGHT[changeClass] ?? 0;
  const typ = TYPE_WEIGHT[changeType] ?? 0;
  const critical = assets.filter((a) => a.criticality === 'Critical').length;
  const assetScore = Math.min(3, critical * 2 + Math.min(1, assets.length * 0.5));
  const score = env + cls + typ + assetScore;
  if (score >= 8) return 'Critical';
  if (score >= 5.5) return 'High';
  if (score >= 3) return 'Medium';
  return 'Low';
}

/** Reuses the root-scope badge.risk.* translations (already bilingual) instead of a local dictionary. */
export const RISK_META: Record<RiskLevel, { labelKey: string; tone: BadgeTone }> = {
  Critical: { labelKey: 'badge.risk.Critical', tone: 'danger' },
  High: { labelKey: 'badge.risk.High', tone: 'danger' },
  Medium: { labelKey: 'badge.risk.Medium', tone: 'warning' },
  Low: { labelKey: 'badge.risk.Low', tone: 'success' }
};

/* ─────────────────────────────────────────────────────────
 * Dynamic technical fields — Step 2 renders these per Change Type.
 * ───────────────────────────────────────────────────────── */

export interface TechFieldDef {
  key: string;
  label: string;
  kind: 'text' | 'textarea' | 'select' | 'toggle';
  options?: string[];
  placeholder?: string;
}

interface TechFieldSpec {
  key: string;
  labelKey: string;
  kind: 'text' | 'textarea' | 'select' | 'toggle';
  options?: string[];
  placeholderKey?: string;
}

const TECHNICAL_FIELD_SPECS: Record<string, TechFieldSpec[]> = {
  'sql-fix': [
    { key: 'sqlScript', labelKey: 'changes.techField.sqlScript', kind: 'textarea', placeholderKey: 'changes.techField.sqlScriptPlaceholder' },
    { key: 'rollbackScript', labelKey: 'changes.techField.rollbackScript', kind: 'textarea', placeholderKey: 'changes.techField.rollbackScriptPlaceholder' },
    { key: 'affectedDatabase', labelKey: 'changes.techField.affectedDatabase', kind: 'text' },
    { key: 'affectedSchema', labelKey: 'changes.techField.affectedSchema', kind: 'text' },
    { key: 'transactionUsed', labelKey: 'changes.techField.transactionUsed', kind: 'toggle' },
    { key: 'estimatedRowCount', labelKey: 'changes.techField.estimatedRowCount', kind: 'text' },
    { key: 'backupRequired', labelKey: 'changes.techField.backupRequired', kind: 'toggle' }
  ],
  'api': [
    { key: 'apiName', labelKey: 'changes.techField.apiName', kind: 'text' },
    { key: 'endpoint', labelKey: 'changes.techField.endpoint', kind: 'text', placeholderKey: 'changes.techField.endpointPlaceholder' },
    { key: 'httpMethod', labelKey: 'changes.techField.httpMethod', kind: 'select', options: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'] },
    { key: 'version', labelKey: 'changes.techField.version', kind: 'text' },
    { key: 'backwardCompatible', labelKey: 'changes.techField.backwardCompatible', kind: 'toggle' },
    { key: 'swaggerUrl', labelKey: 'changes.techField.swaggerUrl', kind: 'text' }
  ],
  'app-deploy': [
    { key: 'repository', labelKey: 'changes.techField.repository', kind: 'text' },
    { key: 'branch', labelKey: 'changes.techField.branch', kind: 'text' },
    { key: 'buildNumber', labelKey: 'changes.techField.buildNumber', kind: 'text' },
    { key: 'artifactVersion', labelKey: 'changes.techField.artifactVersion', kind: 'text' },
    { key: 'deploymentPackage', labelKey: 'changes.techField.deploymentPackage', kind: 'text' },
    { key: 'estimatedDuration', labelKey: 'changes.techField.estimatedDuration', kind: 'text' }
  ],
  'config': [
    { key: 'configArea', labelKey: 'changes.techField.configArea', kind: 'text' },
    { key: 'oldValue', labelKey: 'changes.techField.oldValue', kind: 'text' },
    { key: 'newValue', labelKey: 'changes.techField.newValue', kind: 'text' },
    { key: 'restartRequired', labelKey: 'changes.techField.restartRequired', kind: 'toggle' },
    { key: 'downtimeExpected', labelKey: 'changes.techField.downtimeExpected', kind: 'toggle' }
  ],
  'infra': [
    { key: 'server', labelKey: 'changes.techField.server', kind: 'text' },
    { key: 'operatingSystem', labelKey: 'changes.techField.operatingSystem', kind: 'text' },
    { key: 'affectedServices', labelKey: 'changes.techField.affectedServices', kind: 'textarea' },
    { key: 'maintenanceWindow', labelKey: 'changes.techField.maintenanceWindow', kind: 'text', placeholderKey: 'changes.techField.maintenanceWindowPlaceholder' }
  ]
};

/** Types without a bespoke schema fall back to a shared technical-notes field. */
const GENERIC_TECHNICAL_FIELD_SPECS: TechFieldSpec[] = [
  { key: 'technicalSummary', labelKey: 'changes.techField.technicalSummary', kind: 'textarea', placeholderKey: 'changes.techField.technicalSummaryPlaceholder' },
  { key: 'rollbackPlan', labelKey: 'changes.techField.rollbackPlan', kind: 'textarea', placeholderKey: 'changes.techField.rollbackPlanPlaceholder' }
];

export function technicalFields(changeType: string, t: Translate): TechFieldDef[] {
  const specs = TECHNICAL_FIELD_SPECS[changeType] ?? GENERIC_TECHNICAL_FIELD_SPECS;
  return specs.map((s) => ({
    key: s.key,
    label: t(s.labelKey),
    kind: s.kind,
    options: s.options,
    placeholder: s.placeholderKey ? t(s.placeholderKey) : undefined
  }));
}

/** Revision history row (architecture only — revision engine comes later). */
export interface Revision {
  revision: string;
  createdBy: string;
  createdAt: string;
  description: string;
  status: string;
}

export function detailRevisions(c: Change, t: Translate): Revision[] {
  return [
    { revision: 'v3', createdBy: c.owner, createdAt: c.updatedAt, description: t('changes.detail.revisions.riskUpdated'), status: c.status },
    { revision: 'v2', createdBy: 'Ayşe Yılmaz', createdAt: c.createdAt, description: t('changes.detail.revisions.impactAnalysisAdded'), status: 'InReview' },
    { revision: 'v1', createdBy: c.createdBy, createdAt: c.createdAt, description: t('changes.detail.revisions.initialDraftCreated'), status: 'Draft' }
  ];
}

export function detailTimeline(c: Change, t: Translate, locale: string): TimelineItem[] {
  return [
    { title: t('changes.detail.timeline.created'), time: new Date(c.createdAt).toLocaleDateString(locale), tone: 'info', icon: 'change', description: t('changes.detail.timeline.createdDesc', { createdBy: c.createdBy }) },
    { title: t('changes.detail.timeline.revisionAdded'), time: new Date(c.updatedAt).toLocaleDateString(locale), tone: 'info', icon: 'document' },
    { title: t('changes.detail.timeline.submitted'), time: t('changes.detail.timeline.pending'), tone: 'warning', icon: 'approval' },
    { title: t('changes.detail.timeline.approvalPending'), time: t('changes.detail.timeline.pending'), tone: 'warning', icon: 'approval' },
    { title: t('changes.detail.timeline.executionPending'), time: t('changes.detail.timeline.notScheduled'), tone: 'neutral', icon: 'execution' }
  ];
}

export function detailActivity(c: Change, t: Translate): ActivityItem[] {
  return [
    { actor: c.owner, action: t('changes.detail.activity.createdChange'), time: t('changes.time.daysAgo', { days: 2 }), icon: 'change' },
    { actor: 'Ayşe Yılmaz', action: t('changes.detail.activity.addedImpactAnalysis'), time: t('changes.time.daysAgo', { days: 1 }), icon: 'document' },
    { actor: 'Mehmet Kaya', action: t('changes.detail.activity.leftComment'), time: t('changes.time.hoursAgo', { hours: 4 }), icon: 'inbox' }
  ];
}

export function detailDocuments(): LinkItem[] {
  return [
    { id: 'cd1', label: 'Etki Analizi.pdf', hint: 'PDF · 620 KB', route: '/changes', icon: 'document' },
    { id: 'cd2', label: 'Risk Değerlendirme.xlsx', hint: 'XLSX · 210 KB', route: '/changes', icon: 'document' }
  ];
}

export function relatedReleases(c: Change): LinkItem[] {
  return [
    { id: c.releaseId, label: c.releaseCode, hint: `${c.projectName} · ${c.environmentName}`, route: `/releases/${c.releaseId}`, icon: 'release' }
  ];
}

export function detailUpcoming(t: Translate): LinkItem[] {
  return [
    { id: 'cu1', label: t('changes.detail.upcoming.approvalMeeting'), hint: t('changes.detail.upcoming.tomorrowAt', { time: '14:00' }), route: '/changes', icon: 'approval' },
    { id: 'cu2', label: t('changes.detail.upcoming.impactReview'), hint: t('changes.detail.upcoming.inDays', { days: 2 }), route: '/changes', icon: 'clock' }
  ];
}
