import { Injectable, signal } from '@angular/core';
import { Observable, of } from 'rxjs';

export type SeverityLevel = 'Critical' | 'High' | 'Medium' | 'Low';
export type ValidationResult = 'Passed' | 'Warning' | 'Failed' | 'Skipped';
export type FindingStatus = 'Open' | 'Resolved' | 'Skipped';

/**
 * Reusable rule catalog — the future rule engine will register/execute these.
 * For now the catalog only drives the UI; no rule evaluation happens here.
 */
export interface ValidationRule {
  key: string;
  label: string;
  defaultSeverity: SeverityLevel;
  description: string;
  recommendation: string;
}

export const VALIDATION_RULES: ValidationRule[] = [
  { key: 'missing-rollback', label: 'Geri Alma (Rollback) Eksik', defaultSeverity: 'High', description: 'Değişiklik için tanımlı bir geri alma planı bulunamadı.', recommendation: 'Bir rollback betiği veya planı ekleyin.' },
  { key: 'delete-without-where', label: "WHERE'siz DELETE", defaultSeverity: 'Critical', description: 'DELETE ifadesi WHERE koşulu olmadan kullanılmış.', recommendation: 'DELETE ifadesine uygun bir WHERE koşulu ekleyin.' },
  { key: 'update-without-where', label: "WHERE'siz UPDATE", defaultSeverity: 'Critical', description: 'UPDATE ifadesi WHERE koşulu olmadan kullanılmış.', recommendation: 'UPDATE ifadesine bir WHERE koşulu ekleyin.' },
  { key: 'missing-transaction', label: 'İşlem (Transaction) Eksik', defaultSeverity: 'High', description: 'Betik bir işlem bloğu içine alınmamış.', recommendation: 'Betiği BEGIN/COMMIT/ROLLBACK bloğuna alın.' },
  { key: 'missing-error-handling', label: 'Hata Yönetimi Eksik', defaultSeverity: 'Medium', description: 'Betikte hata yakalama mekanizması bulunmuyor.', recommendation: 'TRY/CATCH veya eşdeğer hata yönetimi ekleyin.' },
  { key: 'missing-test-evidence', label: 'Test Kanıtı Eksik', defaultSeverity: 'High', description: 'Değişikliğe bağlı test kanıtı bulunamadı.', recommendation: 'Test sonuçlarını doküman olarak ekleyin.' },
  { key: 'missing-approval', label: 'Onay Eksik', defaultSeverity: 'Critical', description: 'Gerekli onay adımı henüz tamamlanmamış.', recommendation: 'İlgili onay sürecini tamamlayın.' },
  { key: 'environment-conflict', label: 'Ortam Çakışması', defaultSeverity: 'High', description: 'Aynı ortamda çakışan başka bir değişiklik tespit edildi.', recommendation: 'Çakışan değişikliği çözün veya zamanlamayı ayırın.' },
  { key: 'dependency-conflict', label: 'Bağımlılık Çakışması', defaultSeverity: 'Medium', description: 'Bağımlı bir nesnede çözülmemiş değişiklik var.', recommendation: 'Bağımlılıkları gözden geçirip çözün.' },
  { key: 'duplicate-change', label: 'Yinelenen Değişiklik', defaultSeverity: 'Low', description: 'Benzer içeriğe sahip başka bir değişiklik bulundu.', recommendation: 'Yinelenen kaydı birleştirin veya kapatın.' }
];

export interface ValidationFinding {
  ruleKey: string;
  rule: string;
  result: ValidationResult;
  severity: SeverityLevel;
  description: string;
  recommendation: string;
  status: FindingStatus;
}

export interface Validation {
  id: string;
  code: string;
  changeId: string;
  changeCode: string;
  releaseId: string;
  releaseCode: string;
  projectName: string;
  environmentName: string;
  customerName: string;
  rule: string; // primary rule / summary label
  severity: SeverityLevel; // overall (most severe)
  result: ValidationResult; // overall
  status: string; // Completed / Running
  summary: string;
  executedBy: string;
  executedAt: string;
  findings: ValidationFinding[];
}

export const VALIDATION_RESULTS: ValidationResult[] = ['Passed', 'Warning', 'Failed', 'Skipped'];
export const SEVERITIES: SeverityLevel[] = ['Critical', 'High', 'Medium', 'Low'];

const SEVERITY_RANK: Record<SeverityLevel, number> = { Critical: 4, High: 3, Medium: 2, Low: 1 };
const KEY = 'gms.validations';

function catalog(key: string): ValidationRule {
  return VALIDATION_RULES.find((r) => r.key === key)!;
}

/** Build a finding from the catalog + a per-run result. */
function finding(key: string, result: ValidationResult): ValidationFinding {
  const r = catalog(key);
  const status: FindingStatus = result === 'Passed' ? 'Resolved' : result === 'Skipped' ? 'Skipped' : 'Open';
  return {
    ruleKey: r.key,
    rule: r.label,
    result,
    severity: r.defaultSeverity,
    description: result === 'Passed' ? `${r.label} kontrolü başarıyla geçti.` : r.description,
    recommendation: result === 'Passed' ? '—' : r.recommendation,
    status
  };
}

/** Overall result/severity are derived from the findings (not a live rule engine). */
function build(
  code: string, changeId: string, changeCode: string, releaseId: string, releaseCode: string,
  projectName: string, environmentName: string, customerName: string,
  executedBy: string, executedAt: string, findings: ValidationFinding[]
): Validation {
  const failed = findings.filter((f) => f.result === 'Failed');
  const warnings = findings.filter((f) => f.result === 'Warning');
  const result: ValidationResult = failed.length ? 'Failed' : warnings.length ? 'Warning' : 'Passed';
  const problems = [...failed, ...warnings];
  const severity = problems.reduce<SeverityLevel>((acc, f) => (SEVERITY_RANK[f.severity] > SEVERITY_RANK[acc] ? f.severity : acc), 'Low');
  const primary = problems[0]?.rule ?? 'Tüm kurallar geçti';
  const summary = failed.length
    ? `${failed.length} kritik bulgu değişikliğin yürütülmesini engelliyor.`
    : warnings.length
    ? `${warnings.length} uyarı mevcut; giderilmesi önerilir.`
    : 'Tüm doğrulama kuralları başarıyla geçti; değişiklik yürütmeye hazır.';
  return {
    id: code.toLowerCase(), code, changeId, changeCode, releaseId, releaseCode,
    projectName, environmentName, customerName,
    rule: primary, severity: problems.length ? severity : 'Low', result, status: 'Completed',
    summary, executedBy, executedAt, findings
  };
}

const SEED: Validation[] = [
  build('VAL-2026-001', 'chg-2026-014', 'CHG-2026-014', '07777777-7777-7777-7777-777777777701', 'REL-2026-001', 'EBR Migration', 'PROD', 'Abdi İbrahim', 'System Administrator', '2026-03-04T15:00:00', [
    finding('missing-rollback', 'Warning'),
    finding('missing-transaction', 'Passed'),
    finding('missing-error-handling', 'Warning'),
    finding('missing-test-evidence', 'Passed'),
    finding('duplicate-change', 'Skipped')
  ]),
  build('VAL-2026-002', 'chg-2026-015', 'CHG-2026-015', '07777777-7777-7777-7777-777777777701', 'REL-2026-001', 'EBR Migration', 'PROD', 'Abdi İbrahim', 'Ayşe Yılmaz', '2026-03-05T17:00:00', [
    finding('update-without-where', 'Failed'),
    finding('missing-approval', 'Failed'),
    finding('missing-rollback', 'Warning'),
    finding('environment-conflict', 'Passed')
  ]),
  build('VAL-2026-003', 'chg-2026-017', 'CHG-2026-017', 'a0000000-0000-0000-0000-000000000004', 'REL-2026-004', 'MES Upgrade', 'TEST', 'Bilim İlaç', 'Ali Vural', '2026-03-12T10:00:00', [
    finding('missing-transaction', 'Passed'),
    finding('missing-error-handling', 'Passed'),
    finding('missing-test-evidence', 'Passed'),
    finding('missing-rollback', 'Passed')
  ]),
  build('VAL-2026-004', 'chg-2026-016', 'CHG-2026-016', 'a0000000-0000-0000-0000-000000000003', 'REL-2026-003', 'EBR Migration', 'UAT', 'Abdi İbrahim', 'Mehmet Kaya', '2026-03-06T12:00:00', [
    finding('missing-test-evidence', 'Failed'),
    finding('duplicate-change', 'Warning'),
    finding('missing-error-handling', 'Warning')
  ]),
  build('VAL-2026-005', 'chg-2026-019', 'CHG-2026-019', 'a0000000-0000-0000-0000-000000000006', 'REL-2026-006', 'MES Upgrade', 'PROD', 'Bilim İlaç', 'Ali Vural', '2026-02-27T18:00:00', [
    finding('delete-without-where', 'Passed'),
    finding('missing-rollback', 'Passed'),
    finding('missing-approval', 'Passed'),
    finding('missing-transaction', 'Passed'),
    finding('missing-error-handling', 'Passed')
  ]),
  build('VAL-2026-006', 'chg-2026-018', 'CHG-2026-018', 'a0000000-0000-0000-0000-000000000005', 'REL-2026-005', 'EBR Migration', 'PROD', 'Abdi İbrahim', 'Elif Aydın', '2026-03-15T09:00:00', [
    finding('dependency-conflict', 'Warning'),
    finding('missing-test-evidence', 'Warning'),
    finding('environment-conflict', 'Skipped'),
    finding('missing-rollback', 'Passed')
  ])
];

/**
 * Validation data service. Frontend-only (no rule engine / backend yet) —
 * persisted to localStorage. Observable-based so a real engine can replace it.
 */
@Injectable({ providedIn: 'root' })
export class ValidationService {
  private readonly store = signal<Validation[]>(this.load());

  getValidations(): Observable<Validation[]> {
    return of(this.store());
  }

  getValidation(id: string): Observable<Validation | undefined> {
    return of(this.store().find((v) => v.id === id));
  }

  private load(): Validation[] {
    try {
      const raw = localStorage.getItem(KEY);
      return raw ? (JSON.parse(raw) as Validation[]) : SEED;
    } catch {
      return SEED;
    }
  }
}
