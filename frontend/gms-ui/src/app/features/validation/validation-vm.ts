import { Validation, ValidationResult, ValidationFinding } from '../../core/validation.service';
import { ActivityItem } from '../../shared/ui/activity-feed/activity-feed';
import { TimelineItem } from '../../shared/ui/timeline/timeline';
import { LinkItem } from '../../shared/ui/item-list/item-list';
import { IconName } from '../../shared/icon/icon';
import { BadgeTone } from '../../shared/ui/badge/badge';
import { relativeTime } from '../releases/release-vm';

export { relativeTime };

/** Translator function injected by the component (`transloco.translate`). */
export type Translate = (key: string, params?: Record<string, unknown>) => string;

export function resultIcon(result: ValidationResult): IconName {
  switch (result) {
    case 'Passed': return 'check';
    case 'Warning': return 'clock';
    case 'Failed': return 'close';
    default: return 'inbox';
  }
}

export function resultTone(result: ValidationResult): BadgeTone {
  switch (result) {
    case 'Passed': return 'success';
    case 'Warning': return 'warning';
    case 'Failed': return 'danger';
    default: return 'neutral';
  }
}

export interface ResultCounts {
  passed: number;
  warning: number;
  failed: number;
  skipped: number;
}

export function counts(findings: ValidationFinding[]): ResultCounts {
  return {
    passed: findings.filter((f) => f.result === 'Passed').length,
    warning: findings.filter((f) => f.result === 'Warning').length,
    failed: findings.filter((f) => f.result === 'Failed').length,
    skipped: findings.filter((f) => f.result === 'Skipped').length
  };
}

export function detailTimeline(v: Validation, t: Translate): TimelineItem[] {
  const d = new Date(v.executedAt).toLocaleDateString('tr-TR');
  return [
    { title: t('validation.vmTimeline.created'), time: d, tone: 'info', icon: 'shield', description: t('validation.vmTimeline.createdDesc', { change: v.changeCode }) },
    { title: t('validation.vmTimeline.started'), time: d, tone: 'info', icon: 'execution' },
    { title: t('validation.vmTimeline.completed'), time: d, tone: resultTone(v.result), icon: resultIcon(v.result), description: v.summary },
    { title: t('validation.vmTimeline.recommendationsCreated'), time: v.result === 'Passed' ? t('validation.vmTimeline.notNeeded') : d, tone: 'neutral', icon: 'document' }
  ];
}

export function detailActivity(v: Validation, t: Translate): ActivityItem[] {
  return [
    { actor: v.executedBy, action: t('validation.vmActivity.ranValidation'), time: '2 saat önce', icon: 'shield' },
    { actor: t('validation.vmActivity.systemActor'), action: t('validation.vmActivity.evaluatedRules', { count: v.findings.length }), time: '2 saat önce', icon: 'activity' },
    { actor: 'Mehmet Kaya', action: t('validation.vmActivity.reviewedFinding'), time: '1 saat önce', icon: 'search' }
  ];
}

export function detailDocuments(): LinkItem[] {
  return [
    { id: 'vd1', label: 'Doğrulama Raporu.pdf', hint: 'PDF · 380 KB', route: '/validation', icon: 'document' },
    { id: 'vd2', label: 'Kural Kümesi.json', hint: 'JSON · 12 KB', route: '/validation', icon: 'document' }
  ];
}

export function relatedLinks(v: Validation, t: Translate): LinkItem[] {
  return [
    { id: v.changeId, label: v.changeCode, hint: t('validation.vmLinks.relatedChange'), route: `/changes/${v.changeId}`, icon: 'change' },
    { id: v.releaseId, label: v.releaseCode, hint: t('validation.vmLinks.relatedRelease'), route: `/releases/${v.releaseId}`, icon: 'release' }
  ];
}

export function recentValidations(v: Validation, t: Translate): LinkItem[] {
  const items: LinkItem[] = [
    { id: 'val-2026-003', label: 'VAL-2026-003', hint: `${t('badge.status.Passed')} · MES`, route: '/validation/val-2026-003', icon: 'shield' },
    { id: 'val-2026-002', label: 'VAL-2026-002', hint: `${t('badge.status.Failed')} · EBR`, route: '/validation/val-2026-002', icon: 'shield' }
  ];
  return items.filter((x) => x.id !== v.id);
}

export function upcomingActions(v: Validation): LinkItem[] {
  if (v.result === 'Passed') return [];
  return v.findings
    .filter((f) => f.result !== 'Passed' && f.result !== 'Skipped')
    .slice(0, 3)
    .map((f, i): LinkItem => ({ id: 'ua' + i, label: f.recommendation, hint: f.rule, route: '/validation', icon: 'clock' }));
}
