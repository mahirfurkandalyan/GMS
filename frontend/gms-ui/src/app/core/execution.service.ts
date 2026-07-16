import { Injectable, signal } from '@angular/core';
import { Observable, of } from 'rxjs';
import { IconName } from '../shared/icon/icon';

export type RiskLevel = 'High' | 'Medium' | 'Low';
export type StepStatus = 'pending' | 'running' | 'completed' | 'failed' | 'skipped';
export type LogSeverity = 'info' | 'success' | 'warning' | 'error';

/**
 * Runner catalog — future execution integrations register here. The engine that
 * actually runs steps will map a runner key to an adapter (SQL/PowerShell/REST/…).
 * For now the catalog only drives the UI; nothing is executed.
 */
export interface ExecutionRunner {
  key: string;
  label: string;
  icon: IconName;
}

export const EXECUTION_RUNNERS: ExecutionRunner[] = [
  { key: 'sql', label: 'SQL Runner', icon: 'server' },
  { key: 'powershell', label: 'PowerShell', icon: 'execution' },
  { key: 'rest', label: 'REST API', icon: 'activity' },
  { key: 'ssh', label: 'SSH', icon: 'server' },
  { key: 'winsvc', label: 'Windows Service', icon: 'server' },
  { key: 'linuxsvc', label: 'Linux Service', icon: 'server' },
  { key: 'azuredevops', label: 'Azure DevOps', icon: 'orgchart' },
  { key: 'jenkins', label: 'Jenkins', icon: 'orgchart' },
  { key: 'githubactions', label: 'GitHub Actions', icon: 'orgchart' }
];

export function runnerLabel(key: string): string {
  return EXECUTION_RUNNERS.find((r) => r.key === key)?.label ?? key;
}

export interface ExecutionStep {
  number: number;
  title: string;
  description: string;
  expectedDuration: string;
  status: StepStatus;
  assignedUser: string;
  runner: string;
}

export interface LogEntry {
  timestamp: string;
  step: string;
  message: string;
  severity: LogSeverity;
  createdBy: string;
}

export interface Execution {
  id: string;
  code: string;
  releaseId: string;
  releaseCode: string;
  changeId: string;
  changeCode: string;
  projectName: string;
  environmentName: string;
  customerName: string;
  executor: string;
  currentStep: string;
  progress: number;
  status: string;
  risk: RiskLevel;
  summary: string;
  startedAt: string | null;
  completedAt: string | null;
  createdAt: string;
  steps: ExecutionStep[];
  log: LogEntry[];
}

export const EXECUTION_STATUSES = ['Waiting', 'Ready', 'Running', 'Paused', 'Completed', 'Failed', 'RolledBack', 'Cancelled'];

const KEY = 'gms.executions';

// Generic controlled-implementation plan (same shape every module can reuse).
const PLAN: { title: string; description: string; duration: string; runner: string }[] = [
  { title: 'Ön Kontroller', description: 'Ortam sağlığı, kilit ve önkoşul denetimleri.', duration: '2 dk', runner: 'rest' },
  { title: 'Yedek Alma', description: 'Etkilenen nesnelerin yedeği alınır (geri alma için).', duration: '5 dk', runner: 'powershell' },
  { title: 'Betik Uygulama', description: 'Onaylanan değişiklik betiği hedef ortama uygulanır.', duration: '8 dk', runner: 'sql' },
  { title: 'Servis Yeniden Başlatma', description: 'İlgili servisler kontrollü şekilde yeniden başlatılır.', duration: '3 dk', runner: 'winsvc' },
  { title: 'Doğrulama (Smoke Test)', description: 'Temel sağlık kontrolleri çalıştırılır.', duration: '4 dk', runner: 'rest' }
];

function buildSteps(currentIndex: number, status: string, executor: string): ExecutionStep[] {
  return PLAN.map((p, i) => {
    let st: StepStatus;
    if (status === 'Completed') st = 'completed';
    else if (status === 'Failed' && i === currentIndex) st = 'failed';
    else if (status === 'Cancelled' && i >= currentIndex) st = 'skipped';
    else if (i < currentIndex) st = 'completed';
    else if (i === currentIndex) st = status === 'Running' ? 'running' : status === 'Paused' ? 'running' : 'pending';
    else st = 'pending';
    return { number: i + 1, title: p.title, description: p.description, expectedDuration: p.duration, status: st, assignedUser: executor, runner: p.runner };
  });
}

function buildLog(steps: ExecutionStep[], executor: string, base: string): LogEntry[] {
  const log: LogEntry[] = [];
  const t = (min: number) => new Date(new Date(base).getTime() + min * 60000).toISOString();
  let m = 0;
  for (const s of steps) {
    if (s.status === 'completed') {
      log.push({ timestamp: t(m), step: s.title, message: `${s.title} tamamlandı.`, severity: 'success', createdBy: executor });
      m += 2;
    } else if (s.status === 'running') {
      log.push({ timestamp: t(m), step: s.title, message: `${s.title} çalışıyor…`, severity: 'info', createdBy: executor });
      m += 1;
    } else if (s.status === 'failed') {
      log.push({ timestamp: t(m), step: s.title, message: `${s.title} sırasında hata oluştu.`, severity: 'error', createdBy: executor });
      m += 1;
    }
  }
  return log.reverse();
}

function progressOf(steps: ExecutionStep[]): number {
  const done = steps.filter((s) => s.status === 'completed').length;
  return Math.round((done / steps.length) * 100);
}

function ex(
  code: string, releaseId: string, releaseCode: string, changeId: string, changeCode: string,
  projectName: string, environmentName: string, customerName: string, executor: string, risk: RiskLevel,
  status: string, currentIndex: number, createdAt: string, startedAt: string | null, completedAt: string | null
): Execution {
  const steps = buildSteps(currentIndex, status, executor);
  const progress = status === 'Completed' ? 100 : status === 'Waiting' || status === 'Ready' ? 0 : progressOf(steps);
  const current = steps.find((s) => s.status === 'running')?.title ?? (status === 'Completed' ? 'Tamamlandı' : status === 'Waiting' || status === 'Ready' ? 'Başlatılmadı' : steps[currentIndex]?.title ?? '—');
  const summary =
    status === 'Completed' ? 'Yürütme tüm adımlarıyla başarıyla tamamlandı.'
    : status === 'Failed' ? 'Yürütme bir adımda başarısız oldu; inceleme gerekiyor.'
    : status === 'Running' ? 'Yürütme devam ediyor; adımlar sırayla işleniyor.'
    : status === 'Paused' ? 'Yürütme duraklatıldı; devam ettirilmeyi bekliyor.'
    : status === 'Cancelled' ? 'Yürütme iptal edildi.'
    : 'Yürütme başlatılmayı bekliyor.';
  return {
    id: code.toLowerCase(), code, releaseId, releaseCode, changeId, changeCode,
    projectName, environmentName, customerName, executor, currentStep: current,
    progress, status, risk, summary, startedAt, completedAt, createdAt,
    steps, log: buildLog(steps, executor, startedAt ?? createdAt)
  };
}

const SEED: Execution[] = [
  ex('EXE-2026-001', 'a0000000-0000-0000-0000-000000000005', 'REL-2026-005', 'chg-2026-018', 'CHG-2026-018', 'EBR Migration', 'PROD', 'Abdi İbrahim', 'Ali Vural', 'Medium', 'Running', 2, '2026-03-15T09:00:00', '2026-03-15T09:05:00', null),
  ex('EXE-2026-002', '07777777-7777-7777-7777-777777777701', 'REL-2026-001', 'chg-2026-014', 'CHG-2026-014', 'EBR Migration', 'PROD', 'System Administrator', 'System Administrator', 'High', 'Ready', 0, '2026-03-10T10:00:00', null, null),
  ex('EXE-2026-003', 'a0000000-0000-0000-0000-000000000006', 'REL-2026-006', 'chg-2026-019', 'CHG-2026-019', 'MES Upgrade', 'PROD', 'Bilim İlaç', 'Ali Vural', 'High', 'Completed', 5, '2026-02-27T10:00:00', '2026-02-27T10:10:00', '2026-02-27T10:32:00'),
  ex('EXE-2026-004', 'a0000000-0000-0000-0000-000000000004', 'REL-2026-004', 'chg-2026-017', 'CHG-2026-017', 'MES Upgrade', 'TEST', 'Bilim İlaç', 'Elif Aydın', 'Medium', 'Paused', 3, '2026-03-12T14:00:00', '2026-03-12T14:05:00', null),
  ex('EXE-2026-005', 'a0000000-0000-0000-0000-000000000003', 'REL-2026-003', 'chg-2026-016', 'CHG-2026-016', 'EBR Migration', 'UAT', 'Abdi İbrahim', 'Mehmet Kaya', 'Low', 'Failed', 2, '2026-03-06T13:00:00', '2026-03-06T13:05:00', null),
  ex('EXE-2026-006', '07777777-7777-7777-7777-777777777702', 'REL-2026-002', 'chg-2026-015', 'CHG-2026-015', 'MES Upgrade', 'UAT', 'Bilim İlaç', 'Ayşe Yılmaz', 'Medium', 'Waiting', 0, '2026-03-08T09:00:00', null, null)
];

/**
 * Execution data service. Frontend-only (no execution engine / real scripts) —
 * persisted to localStorage. Actions update local state and append log entries.
 */
@Injectable({ providedIn: 'root' })
export class ExecutionService {
  private readonly store = signal<Execution[]>(this.load());

  getExecutions(): Observable<Execution[]> {
    return of(this.store());
  }

  getExecution(id: string): Observable<Execution | undefined> {
    return of(this.store().find((e) => e.id === id));
  }

  start(id: string): Observable<Execution | undefined> {
    return this.mutate(id, (e) => {
      const now = new Date().toISOString();
      const steps = e.steps.map((s, i) => (i === 0 ? { ...s, status: 'running' as StepStatus } : s));
      return this.withLog({ ...e, status: 'Running', startedAt: e.startedAt ?? now, steps, currentStep: steps[0].title, progress: progressOf(steps) }, steps[0].title, 'Yürütme başlatıldı.', 'info');
    });
  }

  pause(id: string): Observable<Execution | undefined> {
    return this.mutate(id, (e) => this.withLog({ ...e, status: 'Paused' }, e.currentStep, 'Yürütme duraklatıldı.', 'warning'));
  }

  resume(id: string): Observable<Execution | undefined> {
    return this.mutate(id, (e) => this.withLog({ ...e, status: 'Running' }, e.currentStep, 'Yürütme devam ettirildi.', 'info'));
  }

  cancel(id: string): Observable<Execution | undefined> {
    return this.mutate(id, (e) => {
      const steps = e.steps.map((s) => (s.status === 'running' || s.status === 'pending' ? { ...s, status: 'skipped' as StepStatus } : s));
      return this.withLog({ ...e, status: 'Cancelled', steps }, e.currentStep, 'Yürütme iptal edildi.', 'warning');
    });
  }

  private withLog(e: Execution, step: string, message: string, severity: LogSeverity): Execution {
    const entry: LogEntry = { timestamp: new Date().toISOString(), step, message, severity, createdBy: e.executor };
    return { ...e, log: [entry, ...e.log] };
  }

  private mutate(id: string, fn: (e: Execution) => Execution): Observable<Execution | undefined> {
    let result: Execution | undefined;
    this.store.update((list) => list.map((e) => (e.id === id ? (result = fn(e)) : e)));
    this.persist();
    return of(result);
  }

  private load(): Execution[] {
    try {
      const raw = localStorage.getItem(KEY);
      return raw ? (JSON.parse(raw) as Execution[]) : SEED;
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
