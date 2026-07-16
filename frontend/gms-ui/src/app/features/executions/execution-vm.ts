import { Execution, StepStatus, LogSeverity } from '../../core/execution.service';
import { ActivityItem } from '../../shared/ui/activity-feed/activity-feed';
import { TimelineItem } from '../../shared/ui/timeline/timeline';
import { LinkItem } from '../../shared/ui/item-list/item-list';
import { IconName } from '../../shared/icon/icon';
import { BadgeTone } from '../../shared/ui/badge/badge';
import { relativeTime } from '../releases/release-vm';

export { relativeTime };

export function stepIcon(status: StepStatus): IconName {
  switch (status) {
    case 'completed': return 'check';
    case 'running': return 'execution';
    case 'failed': return 'close';
    case 'skipped': return 'chevron-right';
    default: return 'clock';
  }
}

export function stepTone(status: StepStatus): BadgeTone {
  switch (status) {
    case 'completed': return 'success';
    case 'running': return 'info';
    case 'failed': return 'danger';
    case 'skipped': return 'neutral';
    default: return 'neutral';
  }
}

export function stepLabel(status: StepStatus): string {
  switch (status) {
    case 'completed': return 'Tamamlandı';
    case 'running': return 'Çalışıyor';
    case 'failed': return 'Başarısız';
    case 'skipped': return 'Atlandı';
    default: return 'Bekliyor';
  }
}

export function severityTone(sev: LogSeverity): BadgeTone {
  switch (sev) {
    case 'success': return 'success';
    case 'warning': return 'warning';
    case 'error': return 'danger';
    default: return 'info';
  }
}

export function severityLabel(sev: LogSeverity): string {
  switch (sev) {
    case 'success': return 'Başarılı';
    case 'warning': return 'Uyarı';
    case 'error': return 'Hata';
    default: return 'Bilgi';
  }
}

export function detailTimeline(e: Execution): TimelineItem[] {
  const c = new Date(e.createdAt).toLocaleDateString('tr-TR');
  const s = e.startedAt ? new Date(e.startedAt).toLocaleString('tr-TR') : 'Başlatılmadı';
  return [
    { title: 'Yürütme oluşturuldu', time: c, tone: 'info', icon: 'execution', description: `${e.changeCode} için yürütme planlandı.` },
    { title: 'Yürütme başlatıldı', time: s, tone: e.startedAt ? 'info' : 'neutral', icon: 'execution' },
    { title: 'Adım tamamlandı', time: e.progress > 0 ? `%${e.progress}` : 'Bekliyor', tone: 'success', icon: 'check' },
    { title: 'Yürütme duraklatıldı', time: e.status === 'Paused' ? 'Aktif' : '—', tone: e.status === 'Paused' ? 'warning' : 'neutral', icon: 'clock' },
    { title: 'Yürütme tamamlandı', time: e.status === 'Completed' ? new Date(e.completedAt!).toLocaleString('tr-TR') : 'Beklemede', tone: e.status === 'Completed' ? 'success' : 'neutral', icon: 'shield' }
  ];
}

export function detailActivity(e: Execution): ActivityItem[] {
  return [
    { actor: e.executor, action: 'yürütmeyi başlattı', time: '1 saat önce', icon: 'execution' },
    { actor: 'Sistem', action: `${e.steps.filter((s) => s.status === 'completed').length} adım tamamladı`, time: '45 dk önce', icon: 'check' },
    { actor: 'Mehmet Kaya', action: 'yürütmeyi izliyor', time: '20 dk önce', icon: 'search' }
  ];
}

export function detailDocuments(): LinkItem[] {
  return [
    { id: 'ed1', label: 'Yürütme Planı.pdf', hint: 'PDF · 210 KB', route: '/executions', icon: 'document' },
    { id: 'ed2', label: 'Geri Alma Betiği.sql', hint: 'SQL · 8 KB', route: '/executions', icon: 'document' }
  ];
}

export function relatedLinks(e: Execution): LinkItem[] {
  return [
    { id: e.changeId, label: e.changeCode, hint: 'İlgili Değişiklik', route: `/changes/${e.changeId}`, icon: 'change' },
    { id: e.releaseId, label: e.releaseCode, hint: 'İlgili Yayın', route: `/releases/${e.releaseId}`, icon: 'release' },
    { id: 'val', label: 'Doğrulama', hint: 'İlgili doğrulama sonucu', route: '/validation', icon: 'shield' }
  ];
}

export function upcomingSteps(e: Execution): LinkItem[] {
  return e.steps
    .filter((s) => s.status === 'pending')
    .slice(0, 3)
    .map((s): LinkItem => ({ id: 'us' + s.number, label: `${s.number}. ${s.title}`, hint: s.expectedDuration, route: '/executions', icon: 'clock' }));
}
