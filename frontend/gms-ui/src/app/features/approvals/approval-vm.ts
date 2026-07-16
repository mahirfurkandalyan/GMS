import { Approval } from '../../core/approval.service';
import { ActivityItem } from '../../shared/ui/activity-feed/activity-feed';
import { TimelineItem } from '../../shared/ui/timeline/timeline';
import { LinkItem } from '../../shared/ui/item-list/item-list';
import { relativeTime } from '../releases/release-vm';

export { relativeTime, dateLocale } from '../releases/release-vm';

/** Translator function injected by the component (`transloco.translate`). */
export type Translate = (key: string, params?: Record<string, unknown>) => string;

export function detailTimeline(a: Approval, t: Translate): TimelineItem[] {
  const created = new Date(a.requestedAt).toLocaleDateString('tr-TR');
  const done = a.status === 'Approved' || a.status === 'Rejected';
  return [
    { title: t('approvals.timeline.created'), time: created, tone: 'info', icon: 'approval', description: t('approvals.timeline.createdDesc', { by: a.requestedBy }) },
    { title: t('approvals.timeline.assigned'), time: created, tone: 'info', icon: 'user', description: a.currentApprover !== '—' ? a.currentApprover : t('approvals.timeline.assignedAllStages') },
    { title: t('approvals.timeline.viewed'), time: t('approvals.timeline.today'), tone: 'neutral', icon: 'search' },
    { title: a.status === 'Rejected' ? t('approvals.decision.rejected') : t('approvals.decision.approved'), time: done ? t('approvals.flow.stepStatus.completed') : t('approvals.flow.stepStatus.pending'), tone: a.status === 'Rejected' ? 'danger' : a.status === 'Approved' ? 'success' : 'warning', icon: a.status === 'Rejected' ? 'close' : 'check' },
    { title: t('approvals.timeline.processCompleted'), time: a.status === 'Approved' ? t('approvals.flow.stepStatus.completed') : t('approvals.timeline.pendingState'), tone: a.status === 'Approved' ? 'success' : 'neutral', icon: 'shield' }
  ];
}

export function detailActivity(a: Approval, t: Translate): ActivityItem[] {
  return [
    { actor: a.requestedBy, action: t('approvals.vmActivity.createdRequest'), time: '2 gün önce', icon: 'approval' },
    { actor: a.currentApprover !== '—' ? a.currentApprover : t('approvals.vmActivity.approverFallback'), action: t('approvals.vmActivity.viewedRequest'), time: '4 saat önce', icon: 'search' },
    { actor: 'Mehmet Kaya', action: t('approvals.vmActivity.leftComment'), time: '1 saat önce', icon: 'inbox' }
  ];
}

export function detailDocuments(): LinkItem[] {
  return [
    { id: 'ad1', label: 'Onay Formu.pdf', hint: 'PDF · 240 KB', route: '/approvals', icon: 'document' },
    { id: 'ad2', label: 'Etki Analizi.pdf', hint: 'PDF · 620 KB', route: '/approvals', icon: 'document' }
  ];
}

export function pendingActions(a: Approval, t: Translate): LinkItem[] {
  if (a.status !== 'Pending') return [];
  return [
    { id: 'pa1', label: `${a.currentApprover} ${t('approvals.vmLinks.pendingDecisionSuffix')}`, hint: a.stage, route: '/approvals', icon: 'approval' }
  ];
}

export function relatedLinks(a: Approval, t: Translate): LinkItem[] {
  const items: LinkItem[] = [
    { id: a.releaseId, label: a.releaseCode, hint: t('approvals.vmLinks.relatedRelease'), route: `/releases/${a.releaseId}`, icon: 'release' }
  ];
  if (a.changeId && a.changeCode) {
    items.push({ id: a.changeId, label: a.changeCode, hint: t('approvals.vmLinks.relatedChange'), route: `/changes/${a.changeId}`, icon: 'change' });
  }
  return items;
}

export function upcomingSteps(a: Approval): LinkItem[] {
  return a.steps
    .filter((s) => s.status === 'waiting' && s.role !== 'Tamamlandı')
    .slice(0, 3)
    .map((s, i) => ({ id: 'us' + i, label: s.role, hint: s.approver, route: '/approvals', icon: 'clock' }));
}
