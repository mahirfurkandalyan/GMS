import { Injectable, signal } from '@angular/core';
import { Observable, of } from 'rxjs';

export type RiskLevel = 'High' | 'Medium' | 'Low';
export type PriorityLevel = 'Critical' | 'High' | 'Medium' | 'Low';
export type StepStatus = 'approved' | 'pending' | 'waiting' | 'rejected' | 'completed';

export interface ApprovalStep {
  role: string;
  approver: string;
  status: StepStatus;
  date: string | null;
  comment: string | null;
}

export interface Approval {
  id: string;
  code: string;
  title: string;
  status: string; // Pending / Waiting / Approved / Rejected / Expired / Cancelled
  priority: PriorityLevel;
  risk: RiskLevel;
  approvalType: string;
  stage: string;
  currentApprover: string;
  releaseId: string;
  releaseCode: string;
  changeId: string | null;
  changeCode: string | null;
  projectName: string;
  environmentName: string;
  customerName: string;
  businessReason: string;
  requestedBy: string;
  requestedAt: string;
  dueDate: string;
  currentDecision: string;
  steps: ApprovalStep[];
}

export const APPROVAL_STATUSES = ['Pending', 'Waiting', 'Approved', 'Rejected', 'Expired', 'Cancelled'];
export const APPROVAL_TYPES = ['Yayın Onayı', 'Değişiklik Onayı', 'Doğrulama Onayı'];

// Governance approval chain (roles + assigned approvers).
const CHAIN: { role: string; approver: string }[] = [
  { role: 'Talep Eden', approver: 'Requester User' },
  { role: 'Mimari İnceleme', approver: 'Architect User' },
  { role: 'QA Onayı', approver: 'QA Specialist' },
  { role: 'Yayın Yöneticisi', approver: 'System Administrator' }
];

const KEY = 'gms.approvals';

/** Build the step list from the current stage index and overall status. */
function buildSteps(stageIndex: number, status: string, base: string): ApprovalStep[] {
  const steps: ApprovalStep[] = CHAIN.map((c, i) => {
    let st: StepStatus;
    let date: string | null = null;
    let comment: string | null = null;
    if (status === 'Approved') {
      st = 'approved';
      date = base;
      comment = 'Onaylandı.';
    } else if (i < stageIndex) {
      st = 'approved';
      date = base;
      comment = 'Uygun bulundu.';
    } else if (i === stageIndex) {
      st = status === 'Rejected' ? 'rejected' : status === 'Expired' ? 'waiting' : status === 'Waiting' ? 'waiting' : 'pending';
      if (status === 'Rejected') comment = 'Revizyon gerekiyor.';
    } else {
      st = 'waiting';
    }
    return { role: c.role, approver: c.approver, status: st, date, comment };
  });
  steps.push({
    role: 'Tamamlandı',
    approver: '—',
    status: status === 'Approved' ? 'completed' : 'waiting',
    date: status === 'Approved' ? base : null,
    comment: null
  });
  return steps;
}

function ap(
  code: string, title: string, status: string, priority: PriorityLevel, risk: RiskLevel,
  approvalType: string, stageIndex: number, releaseId: string, releaseCode: string,
  changeId: string | null, changeCode: string | null, projectName: string, environmentName: string,
  customerName: string, businessReason: string, requestedBy: string, requestedAt: string, dueDate: string
): Approval {
  const decision = status === 'Approved' ? 'Onaylandı' : status === 'Rejected' ? 'Reddedildi' : status === 'Expired' ? 'Süresi doldu' : status === 'Waiting' ? 'Sırada bekliyor' : 'Karar bekleniyor';
  const stage = CHAIN[Math.min(stageIndex, CHAIN.length - 1)].role;
  const approver = CHAIN[Math.min(stageIndex, CHAIN.length - 1)].approver;
  return {
    id: code.toLowerCase(), code, title, status, priority, risk, approvalType,
    stage, currentApprover: status === 'Approved' ? '—' : approver,
    releaseId, releaseCode, changeId, changeCode, projectName, environmentName, customerName,
    businessReason, requestedBy, requestedAt, dueDate, currentDecision: decision,
    steps: buildSteps(stageIndex, status, requestedAt)
  };
}

const SEED: Approval[] = [
  ap('APR-2026-001', 'EBR PROD Yayın Onayı', 'Pending', 'High', 'Medium', 'Yayın Onayı', 1, '07777777-7777-7777-7777-777777777701', 'REL-2026-001', 'chg-2026-014', 'CHG-2026-014', 'EBR Migration', 'PROD', 'Abdi İbrahim', 'EBR Migration üretim yayını için mimari onay gereklidir.', 'System Administrator', '2026-03-04T10:00:00', '2026-07-12T17:00:00'),
  ap('APR-2026-002', 'Yetki Matrisi Değişiklik Onayı', 'Waiting', 'Critical', 'High', 'Değişiklik Onayı', 2, '07777777-7777-7777-7777-777777777701', 'REL-2026-001', 'chg-2026-015', 'CHG-2026-015', 'EBR Migration', 'PROD', 'Abdi İbrahim', 'Denetim bulgusu sonrası yetki matrisi değişikliği QA onayı bekliyor.', 'Furkan Demir', '2026-03-05T09:00:00', '2026-07-10T17:00:00'),
  ap('APR-2026-003', 'MES UAT Doğrulama Onayı', 'Approved', 'Medium', 'Medium', 'Doğrulama Onayı', 3, 'a0000000-0000-0000-0000-000000000004', 'REL-2026-004', 'chg-2026-017', 'CHG-2026-017', 'MES Upgrade', 'TEST', 'Bilim İlaç', 'MES UAT doğrulama sonuçları onaylandı.', 'Ali Vural', '2026-03-08T14:00:00', '2026-06-30T17:00:00'),
  ap('APR-2026-004', 'Rapor Şablonu Onayı', 'Rejected', 'Low', 'Low', 'Değişiklik Onayı', 1, 'a0000000-0000-0000-0000-000000000003', 'REL-2026-003', 'chg-2026-016', 'CHG-2026-016', 'EBR Migration', 'UAT', 'Abdi İbrahim', 'Rapor şablonu talebi eksik gerekçe nedeniyle reddedildi.', 'Mehmet Kaya', '2026-03-06T11:00:00', '2026-06-25T17:00:00'),
  ap('APR-2026-005', 'Güvenlik Yaması Onayı', 'Approved', 'High', 'High', 'Değişiklik Onayı', 4, 'a0000000-0000-0000-0000-000000000006', 'REL-2026-006', 'chg-2026-019', 'CHG-2026-019', 'MES Upgrade', 'PROD', 'Bilim İlaç', 'Kritik güvenlik yaması tüm aşamalardan onay aldı.', 'Ali Vural', '2026-02-20T09:00:00', '2026-02-27T17:00:00'),
  ap('APR-2026-006', 'API Deprecation Onayı', 'Expired', 'Medium', 'Medium', 'Değişiklik Onayı', 2, 'a0000000-0000-0000-0000-000000000004', 'REL-2026-004', null, null, 'MES Upgrade', 'TEST', 'Bilim İlaç', 'Onay süresi dolduğu için işlem yenilenmelidir.', 'Ali Vural', '2026-02-01T09:00:00', '2026-02-15T17:00:00')
];

/**
 * Approval data service. Frontend-only (no backend / workflow engine yet) —
 * persisted to localStorage. Decisions update local state so the experience
 * feels real; a real API + workflow engine can replace this without UI changes.
 */
@Injectable({ providedIn: 'root' })
export class ApprovalService {
  private readonly store = signal<Approval[]>(this.load());

  getApprovals(): Observable<Approval[]> {
    return of(this.store());
  }

  getApproval(id: string): Observable<Approval | undefined> {
    return of(this.store().find((a) => a.id === id));
  }

  approve(id: string, comment: string): Observable<Approval | undefined> {
    return this.mutate(id, (a) => {
      const idx = a.steps.findIndex((s) => s.status === 'pending');
      if (idx < 0) return a;
      const now = new Date().toISOString();
      const steps = a.steps.map((s, i) =>
        i === idx ? { ...s, status: 'approved' as StepStatus, date: now, comment: comment || 'Onaylandı.' } : s
      );
      const nextIdx = steps.findIndex((s, i) => i > idx && s.role !== 'Tamamlandı' && s.status === 'waiting');
      if (nextIdx >= 0) {
        steps[nextIdx] = { ...steps[nextIdx], status: 'pending' };
        return { ...a, steps, stage: steps[nextIdx].role, currentApprover: steps[nextIdx].approver, currentDecision: 'Karar bekleniyor', status: 'Pending' };
      }
      const completed = steps.map((s) => (s.role === 'Tamamlandı' ? { ...s, status: 'completed' as StepStatus, date: now } : s));
      return { ...a, steps: completed, status: 'Approved', currentApprover: '—', currentDecision: 'Onaylandı' };
    });
  }

  reject(id: string, comment: string): Observable<Approval | undefined> {
    return this.mutate(id, (a) => {
      const idx = a.steps.findIndex((s) => s.status === 'pending');
      const now = new Date().toISOString();
      const steps = a.steps.map((s, i) =>
        i === idx ? { ...s, status: 'rejected' as StepStatus, date: now, comment: comment || 'Reddedildi.' } : s
      );
      return { ...a, steps, status: 'Rejected', currentDecision: 'Reddedildi' };
    });
  }

  requestRevision(id: string, comment: string): Observable<Approval | undefined> {
    return this.mutate(id, (a) => {
      const idx = a.steps.findIndex((s) => s.status === 'pending');
      const steps = a.steps.map((s, i) =>
        i === idx ? { ...s, status: 'waiting' as StepStatus, comment: comment || 'Revizyon istendi.' } : s
      );
      return { ...a, steps, status: 'Waiting', currentDecision: 'Revizyon istendi' };
    });
  }

  private mutate(id: string, fn: (a: Approval) => Approval): Observable<Approval | undefined> {
    let result: Approval | undefined;
    this.store.update((list) =>
      list.map((a) => {
        if (a.id !== id) return a;
        result = fn(a);
        return result;
      })
    );
    this.persist();
    return of(result);
  }

  private load(): Approval[] {
    try {
      const raw = localStorage.getItem(KEY);
      return raw ? (JSON.parse(raw) as Approval[]) : SEED;
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
