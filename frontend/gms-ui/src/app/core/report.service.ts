import { Injectable, inject } from '@angular/core';
import { Observable, combineLatest, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ReleaseService, Release } from './release.service';
import { ChangeService, Change } from './change.service';
import { ApprovalService, Approval } from './approval.service';
import { ExecutionService, Execution } from './execution.service';
import { AssetService, GmsAsset } from './asset.service';
import { ChartDatum } from '../shared/ui/chart/chart';
import { ActivityItem } from '../shared/ui/activity-feed/activity-feed';
import { LinkItem } from '../shared/ui/item-list/item-list';
import { StatTone } from '../shared/ui/stat/stat';
import { IconName } from '../shared/icon/icon';
import { BadgeTone, STATUS_BADGES, RISK_BADGES } from '../shared/ui/badge/badge';

/**
 * Executive analytics aggregator. Derives every figure from the existing seeded
 * domain data (releases / changes / approvals / executions / assets) — no
 * fabricated statistics. Frontend-only; a real BI/analytics API can replace the
 * service body without touching the report components.
 */

export interface Kpi {
  key: string;
  label: string;
  value: string | number;
  delta?: string;
  tone?: StatTone;
  icon: IconName;
  spark?: number[];
}

export interface TopIssue {
  id: string;
  object: string;
  type: string;
  priority: string;
  status: string;
  owner: string;
  updatedAt: string;
  route: string;
}

export interface ReportSnapshot {
  kpis: Kpi[];
  // Overview charts
  releaseTrend: ChartDatum[];
  changeTrend: ChartDatum[];
  riskDistribution: ChartDatum[];
  approvalStatus: ChartDatum[];
  executionSuccess: ChartDatum[];
  environmentActivity: ChartDatum[];
  topIssues: TopIssue[];
  activity: ActivityItem[];
  // Release report
  releaseStatus: ChartDatum[];
  upcomingReleases: LinkItem[];
  completedReleases: LinkItem[];
  delayedReleases: LinkItem[];
  releaseCounts: { total: number; upcoming: number; completed: number; delayed: number };
  // Change report
  changeByRisk: ChartDatum[];
  changeByEnv: ChartDatum[];
  changeByStatus: ChartDatum[];
  openChanges: TopIssue[];
  // Reusable filter option lists
  filterOptions: { projects: string[]; environments: string[]; releases: string[]; owners: string[] };
}

const TR_MONTHS = ['Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara'];

function statusTone(status: string): BadgeTone {
  return STATUS_BADGES[status]?.tone ?? 'neutral';
}
function riskTone(risk: string): BadgeTone {
  return RISK_BADGES[risk]?.tone ?? 'neutral';
}
function envTone(env: string): BadgeTone {
  return env === 'PROD' ? 'danger' : env === 'UAT' || env === 'PREPROD' ? 'warning' : env === 'TEST' ? 'info' : 'neutral';
}

/** Count occurrences of a key across a list, preserving a preferred order. */
function countBy<T>(list: T[], key: (t: T) => string, order?: string[]): { label: string; value: number }[] {
  const map = new Map<string, number>();
  for (const item of list) {
    const k = key(item);
    map.set(k, (map.get(k) ?? 0) + 1);
  }
  const keys = order ? order.filter((k) => map.has(k)) : [...map.keys()];
  for (const k of map.keys()) if (!keys.includes(k)) keys.push(k);
  return keys.map((k) => ({ label: k, value: map.get(k) ?? 0 }));
}

/** Group by created-month → monthly counts (chronological). */
function monthlyTrend(items: { createdAt: string }[]): ChartDatum[] {
  const map = new Map<string, number>();
  for (const it of items) {
    const d = new Date(it.createdAt);
    if (isNaN(d.getTime())) continue;
    const k = `${d.getFullYear()}-${String(d.getMonth()).padStart(2, '0')}`;
    map.set(k, (map.get(k) ?? 0) + 1);
  }
  const sorted = [...map.entries()].sort((a, b) => a[0].localeCompare(b[0]));
  return sorted.map(([k, v]) => ({ label: TR_MONTHS[+k.split('-')[1]], value: v, tone: 'info' as BadgeTone }));
}

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly releases = inject(ReleaseService);
  private readonly changes = inject(ChangeService);
  private readonly approvals = inject(ApprovalService);
  private readonly executions = inject(ExecutionService);
  private readonly assets = inject(AssetService);

  getSnapshot(): Observable<ReportSnapshot> {
    return combineLatest([
      this.releases.getReleases().pipe(catchError(() => of([] as Release[]))),
      this.changes.getChanges().pipe(catchError(() => of([] as Change[]))),
      this.approvals.getApprovals().pipe(catchError(() => of([] as Approval[]))),
      this.executions.getExecutions().pipe(catchError(() => of([] as Execution[]))),
      this.assets.getAssets().pipe(catchError(() => of([] as GmsAsset[])))
    ]).pipe(map(([rel, chg, apr, exe, ast]) => this.build(rel, chg, apr, exe, ast)));
  }

  private build(rel: Release[], chg: Change[], apr: Approval[], exe: Execution[], ast: GmsAsset[]): ReportSnapshot {
    const openChangeStatuses = ['Draft', 'Open', 'InReview', 'Approval', 'Pending'];
    const openChanges = chg.filter((c) => openChangeStatuses.includes(c.status));
    const pendingApprovals = apr.filter((a) => ['Pending', 'Waiting'].includes(a.status));
    const failedExecutions = exe.filter((e) => e.status === 'Failed');
    const criticalRisks = [...chg, ...apr, ...exe].filter((x) => x.risk === 'High' || x.risk === 'Critical').length
      + ast.filter((a) => a.criticality === 'Critical').length;

    const kpis: Kpi[] = [
      { key: 'releases', label: 'Toplam Yayın', value: rel.length, icon: 'release', tone: 'neutral', spark: monthlyTrend(rel).map((d) => d.value) },
      { key: 'open-changes', label: 'Açık Değişiklik', value: openChanges.length, icon: 'change', tone: 'neutral' },
      { key: 'pending-approvals', label: 'Bekleyen Onay', value: pendingApprovals.length, icon: 'approval', tone: pendingApprovals.length ? 'down' : 'neutral' },
      { key: 'failed-exec', label: 'Başarısız Yürütme', value: failedExecutions.length, icon: 'execution', tone: failedExecutions.length ? 'down' : 'up' },
      { key: 'critical-risks', label: 'Kritik Risk', value: criticalRisks, icon: 'shield', tone: criticalRisks ? 'down' : 'up' },
      { key: 'avg-approval', label: 'Ort. Onay Süresi', value: this.avgApprovalDays(apr), icon: 'clock', tone: 'neutral' }
    ];

    // Environment activity across all governance objects.
    const envItems = [
      ...chg.map((c) => c.environmentName),
      ...exe.map((e) => e.environmentName),
      ...rel.map((r) => r.environmentName)
    ].filter(Boolean);
    const environmentActivity = countBy(envItems, (e) => e, ['DEV', 'TEST', 'UAT', 'PREPROD', 'PROD'])
      .map((d) => ({ ...d, tone: envTone(d.label) }));

    const execSuccess = this.groupTones(
      countBy(exe, (e) => e.status, ['Completed', 'Running', 'Failed', 'Cancelled']),
      statusTone
    );

    const snapshot: ReportSnapshot = {
      kpis,
      releaseTrend: monthlyTrend(rel),
      changeTrend: monthlyTrend(chg).map((d) => ({ ...d, tone: 'success' as BadgeTone })),
      riskDistribution: this.groupTones(countBy(chg, (c) => c.risk, ['Critical', 'High', 'Medium', 'Low']), riskTone),
      approvalStatus: this.groupTones(countBy(apr, (a) => a.status), statusTone),
      executionSuccess: execSuccess,
      environmentActivity,
      topIssues: this.topIssues(chg, exe, apr),
      activity: this.activity(rel, chg, apr, exe),
      // Release report
      releaseStatus: this.groupTones(countBy(rel, (r) => r.status), statusTone),
      upcomingReleases: this.releaseLinks(rel.filter((r) => this.isUpcoming(r))),
      completedReleases: this.releaseLinks(rel.filter((r) => r.status === 'Completed')),
      delayedReleases: this.releaseLinks(rel.filter((r) => this.isDelayed(r))),
      releaseCounts: {
        total: rel.length,
        upcoming: rel.filter((r) => this.isUpcoming(r)).length,
        completed: rel.filter((r) => r.status === 'Completed').length,
        delayed: rel.filter((r) => this.isDelayed(r)).length
      },
      // Change report
      changeByRisk: this.groupTones(countBy(chg, (c) => c.risk, ['Critical', 'High', 'Medium', 'Low']), riskTone),
      changeByEnv: countBy(chg, (c) => c.environmentName, ['DEV', 'TEST', 'UAT', 'PREPROD', 'PROD']).map((d) => ({ ...d, tone: envTone(d.label) })),
      changeByStatus: this.groupTones(countBy(chg, (c) => c.status), statusTone),
      openChanges: openChanges.map((c) => this.changeIssue(c)),
      filterOptions: {
        projects: [...new Set([...rel.map((r) => r.projectName), ...chg.map((c) => c.projectName)])].filter(Boolean).sort(),
        environments: [...new Set(envItems)].sort(),
        releases: [...new Set(rel.map((r) => r.name))].sort(),
        owners: [...new Set([...chg.map((c) => c.owner), ...exe.map((e) => e.executor)])].filter(Boolean).sort()
      }
    };
    return snapshot;
  }

  private groupTones(rows: { label: string; value: number }[], toneOf: (label: string) => BadgeTone): ChartDatum[] {
    return rows.map((r) => ({ label: STATUS_BADGES[r.label]?.label ?? RISK_BADGES[r.label]?.label ?? r.label, value: r.value, tone: toneOf(r.label) }));
  }

  private avgApprovalDays(apr: Approval[]): string {
    const approved = apr.filter((a) => a.status === 'Approved');
    const durations: number[] = [];
    for (const a of approved) {
      const start = new Date(a.requestedAt).getTime();
      const dates = a.steps.map((s) => (s.date ? new Date(s.date).getTime() : 0)).filter((t) => t > 0);
      const end = dates.length ? Math.max(...dates) : 0;
      if (end > start) durations.push((end - start) / 86400000);
    }
    if (!durations.length) return '—';
    const avg = durations.reduce((s, d) => s + d, 0) / durations.length;
    return `${avg.toFixed(1)} gün`;
  }

  private topIssues(chg: Change[], exe: Execution[], apr: Approval[]): TopIssue[] {
    const issues: TopIssue[] = [];
    for (const c of chg.filter((x) => x.risk === 'High')) issues.push(this.changeIssue(c));
    for (const e of exe.filter((x) => x.status === 'Failed' || x.risk === 'High')) {
      issues.push({ id: e.id, object: e.code, type: 'Yürütme', priority: e.risk, status: e.status, owner: e.executor, updatedAt: e.createdAt, route: `/executions/${e.id}` });
    }
    for (const a of apr.filter((x) => x.status === 'Expired' || x.status === 'Rejected')) {
      issues.push({ id: a.id, object: a.code, type: 'Onay', priority: a.priority, status: a.status, owner: a.currentApprover, updatedAt: a.requestedAt, route: `/approvals/${a.id}` });
    }
    return issues
      .sort((x, y) => new Date(y.updatedAt).getTime() - new Date(x.updatedAt).getTime())
      .slice(0, 8);
  }

  private changeIssue(c: Change): TopIssue {
    return { id: c.id, object: c.code, type: 'Değişiklik', priority: c.priority, status: c.status, owner: c.owner, updatedAt: c.updatedAt, route: `/changes/${c.id}` };
  }

  private activity(rel: Release[], chg: Change[], apr: Approval[], exe: Execution[]): ActivityItem[] {
    const events: { at: number; item: ActivityItem }[] = [];
    for (const r of rel.slice(0, 4)) events.push({ at: new Date(r.createdAt).getTime(), item: { actor: r.createdByUserName, action: `${r.name} yayınını oluşturdu`, time: this.rel(r.createdAt), icon: 'release' } });
    for (const c of chg.slice(0, 4)) events.push({ at: new Date(c.updatedAt).getTime(), item: { actor: c.owner, action: `${c.code} değişikliğini güncelledi`, time: this.rel(c.updatedAt), icon: 'change' } });
    for (const a of apr.slice(0, 3)) events.push({ at: new Date(a.requestedAt).getTime(), item: { actor: a.requestedBy, action: `${a.code} için onay talep etti`, time: this.rel(a.requestedAt), icon: 'approval' } });
    for (const e of exe.slice(0, 3)) events.push({ at: new Date(e.createdAt).getTime(), item: { actor: e.executor, action: `${e.code} yürütmesini başlattı`, time: this.rel(e.createdAt), icon: 'execution' } });
    return events.sort((a, b) => b.at - a.at).slice(0, 8).map((e) => e.item);
  }

  private releaseLinks(rel: Release[]): LinkItem[] {
    return rel.map((r): LinkItem => ({
      id: r.id,
      label: r.name,
      hint: `${r.projectName} · ${r.environmentName}`,
      route: `/releases/${r.id}`,
      icon: 'release'
    }));
  }

  private isUpcoming(r: Release): boolean {
    return !!r.plannedDate && ['Planned', 'Planning', 'Approval', 'Validation', 'Draft'].includes(r.status);
  }
  private isDelayed(r: Release): boolean {
    if (!r.plannedDate) return false;
    const planned = new Date(r.plannedDate).getTime();
    return planned < Date.now() && r.status !== 'Completed';
  }

  private rel(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const d = Math.floor(diff / 86400000);
    if (d <= 0) return 'bugün';
    if (d === 1) return 'dün';
    if (d < 30) return `${d} gün önce`;
    const m = Math.floor(d / 30);
    return `${m} ay önce`;
  }
}
