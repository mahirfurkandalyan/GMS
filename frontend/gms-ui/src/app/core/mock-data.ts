import { Release } from './release.service';
import { Project, AppEnvironment } from './catalog.service';

/**
 * Offline demo dataset. Used ONLY as a `catchError` fallback when the backend
 * is unreachable — real API data is always preferred. Mirrors the EF seed so
 * IDs stay consistent (list → detail navigation works offline). No API change.
 */

const PROJ_EBR = 'e5555555-5555-5555-5555-555555555501';
const PROJ_MES = 'e5555555-5555-5555-5555-555555555502';

export const MOCK_PROJECTS: Project[] = [
  { id: PROJ_EBR, customerId: 'c-abdi', customerName: 'Abdi İbrahim', name: 'EBR Migration', code: 'EBR-MIG', description: 'Elektronik Batch Record geçiş projesi.', status: 'Active' },
  { id: PROJ_MES, customerId: 'c-bilim', customerName: 'Bilim İlaç', name: 'MES Upgrade', code: 'MES-UPG', description: 'MES sürüm yükseltme projesi.', status: 'Active' }
];

export const MOCK_ENVIRONMENTS: AppEnvironment[] = [
  { id: 'env-ebr-dev', projectId: PROJ_EBR, projectName: 'EBR Migration', name: 'DEV', type: 'Geliştirme', status: 'Active' },
  { id: 'env-ebr-test', projectId: PROJ_EBR, projectName: 'EBR Migration', name: 'TEST', type: 'Test', status: 'Active' },
  { id: 'env-ebr-uat', projectId: PROJ_EBR, projectName: 'EBR Migration', name: 'UAT', type: 'Kullanıcı Kabul', status: 'Active' },
  { id: 'env-ebr-prod', projectId: PROJ_EBR, projectName: 'EBR Migration', name: 'PROD', type: 'Üretim', status: 'Active' },
  { id: 'env-mes-dev', projectId: PROJ_MES, projectName: 'MES Upgrade', name: 'DEV', type: 'Geliştirme', status: 'Active' },
  { id: 'env-mes-test', projectId: PROJ_MES, projectName: 'MES Upgrade', name: 'TEST', type: 'Test', status: 'Active' },
  { id: 'env-mes-uat', projectId: PROJ_MES, projectName: 'MES Upgrade', name: 'UAT', type: 'Kullanıcı Kabul', status: 'Active' },
  { id: 'env-mes-prod', projectId: PROJ_MES, projectName: 'MES Upgrade', name: 'PROD', type: 'Üretim', status: 'Active' }
];

function rel(
  id: string, name: string, projectName: string, projectId: string, env: string,
  version: string, status: string, owner: string, planned: string | null, created: string, desc: string
): Release {
  return {
    id, projectId, projectName, environmentId: `${projectId}-${env}`, environmentName: env,
    name, version, description: desc, plannedDate: planned, status,
    createdAt: created, createdByUserId: 'u', createdByUserName: owner
  };
}

export const MOCK_RELEASES: Release[] = [
  rel('07777777-7777-7777-7777-777777777701', 'REL-2026-001', 'EBR Migration', PROJ_EBR, 'PROD', 'v1.0', 'Planned', 'System Administrator', '2026-07-15T10:00:00', '2026-01-01T00:00:00', 'EBR Migration üretim yayını.'),
  rel('07777777-7777-7777-7777-777777777702', 'REL-2026-002', 'MES Upgrade', PROJ_MES, 'UAT', 'v0.9', 'Draft', 'Requester User', '2026-08-01T09:00:00', '2026-01-01T00:00:00', 'MES Upgrade UAT taslak yayını.'),
  rel('a0000000-0000-0000-0000-000000000003', 'REL-2026-003', 'EBR Migration', PROJ_EBR, 'UAT', 'v1.1', 'Validation', 'Architect User', '2026-07-20T10:00:00', '2026-02-10T12:00:00', 'Doğrulama aşamasındaki yayın.'),
  rel('a0000000-0000-0000-0000-000000000004', 'REL-2026-004', 'MES Upgrade', PROJ_MES, 'TEST', 'v2.0', 'Approval', 'QA Specialist', '2026-07-25T14:00:00', '2026-03-05T09:30:00', 'Onay bekleyen büyük sürüm.'),
  rel('a0000000-0000-0000-0000-000000000005', 'REL-2026-005', 'EBR Migration', PROJ_EBR, 'PROD', 'v1.2', 'Executing', 'System Administrator', '2026-07-10T08:00:00', '2026-04-01T08:00:00', 'Üretimde yürütülen yayın.'),
  rel('a0000000-0000-0000-0000-000000000006', 'REL-2026-006', 'MES Upgrade', PROJ_MES, 'PROD', 'v1.5', 'Completed', 'Executor User', '2026-06-01T10:00:00', '2026-05-01T10:00:00', 'Tamamlanmış üretim yayını.'),
  rel('a0000000-0000-0000-0000-000000000007', 'REL-2026-007', 'EBR Migration', PROJ_EBR, 'DEV', 'v0.5', 'Planning', 'Architect User', '2026-09-01T10:00:00', '2026-06-20T15:00:00', 'Planlama aşamasında.')
];
