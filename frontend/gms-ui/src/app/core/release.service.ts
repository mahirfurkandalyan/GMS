import { Injectable, signal } from '@angular/core';
import { Observable, of } from 'rxjs';
import { MOCK_RELEASES } from './mock-data';

export type ReleaseRisk = 'Critical' | 'High' | 'Medium' | 'Low';

/** A change included in a release plan (ordered). Mirrors the Change model. */
export interface ReleaseChangeRef {
  id: string;
  code: string;
  title: string;
  risk: string;
  priority: string;
  owner: string;
  changeClass: string;
  changeType: string;
  estimatedMinutes: number;
}

/**
 * LEGACY mock Release store (localStorage). As of the Release Planning backend integration sprint the
 * Release screens (list / detail / wizard) NO LONGER use this — they talk to the real backend via
 * ReleaseApiService. This mock now survives ONLY as an interim data source for modules that are
 * intentionally NOT migrated yet (Dashboard, Hub, Document list, Reports) and for the shared
 * relativeTime/dateLocale helpers in release-vm.ts. It will be removed when those modules are
 * integrated. Do not wire new Release feature code to this service.
 */
export interface Release {
  id: string;
  projectId: string;
  projectName: string;
  environmentId: string;
  environmentName: string;
  name: string; // release code, e.g. REL-2026-001
  version: string;
  description: string;
  plannedDate: string | null;
  status: string;
  createdAt: string;
  createdByUserId: string;
  createdByUserName: string;
  // ── Wizard-produced fields (optional so legacy seed stays valid) ──
  title?: string;
  customerName?: string;
  releaseType?: string;
  deploymentWindow?: string;
  releaseManager?: string;
  businessGoal?: string;
  risk?: ReleaseRisk;
  changes?: ReleaseChangeRef[];
  // Deployment planning
  rollbackCoordinator?: string;
  communicationPlan?: string;
  businessOwner?: string;
  technicalOwner?: string;
  downtimeExpected?: boolean;
  estimatedDowntime?: string;
  deploymentNotes?: string;
}

export const RELEASE_TYPES: { key: string; label: string }[] = [
  { key: 'major', label: 'Majör Yayın' },
  { key: 'minor', label: 'Minör Yayın' },
  { key: 'patch', label: 'Yama (Patch)' },
  { key: 'hotfix', label: 'Acil Düzeltme (Hotfix)' },
  { key: 'emergency', label: 'Acil Yayın' }
];
export function releaseTypeLabel(key: string): string {
  return RELEASE_TYPES.find((t) => t.key === key)?.label ?? key;
}

/** Everything the wizard collects (server fields added on create). */
export interface CreateReleaseInput {
  title: string;
  version: string;
  projectId: string;
  projectName: string;
  customerName: string;
  environmentId: string;
  environmentName: string;
  releaseType: string;
  plannedDate: string | null;
  deploymentWindow: string;
  releaseManager: string;
  description: string;
  businessGoal: string;
  risk: ReleaseRisk;
  changes: ReleaseChangeRef[];
  rollbackCoordinator: string;
  communicationPlan: string;
  businessOwner: string;
  technicalOwner: string;
  downtimeExpected: boolean;
  estimatedDowntime: string;
  deploymentNotes: string;
  createdByUserId: string;
  createdByUserName: string;
}

const KEY = 'gms.releases';

@Injectable({ providedIn: 'root' })
export class ReleaseService {
  private readonly store = signal<Release[]>(this.load());
  private seq = 100;

  getReleases(): Observable<Release[]> {
    return of(this.store());
  }

  getRelease(id: string): Observable<Release> {
    const found = this.store().find((r) => r.id === id);
    // Keep the Observable<Release> contract; components handle the error branch.
    return found ? of(found) : of(undefined as unknown as Release);
  }

  /** Create a release plan from the wizard. `status` is 'Draft' by default. */
  createPlan(input: CreateReleaseInput, status: 'Draft' = 'Draft'): Observable<Release> {
    const n = String(7 + (this.seq++ % 900)).padStart(3, '0');
    const now = new Date().toISOString();
    const release: Release = {
      id: 'rel-' + this.seq,
      name: `REL-2026-${n}`,
      projectId: input.projectId,
      projectName: input.projectName,
      environmentId: input.environmentId,
      environmentName: input.environmentName,
      version: input.version,
      description: input.description,
      plannedDate: input.plannedDate,
      status,
      createdAt: now,
      createdByUserId: input.createdByUserId,
      createdByUserName: input.createdByUserName,
      title: input.title,
      customerName: input.customerName,
      releaseType: input.releaseType,
      deploymentWindow: input.deploymentWindow,
      releaseManager: input.releaseManager,
      businessGoal: input.businessGoal,
      risk: input.risk,
      changes: input.changes,
      rollbackCoordinator: input.rollbackCoordinator,
      communicationPlan: input.communicationPlan,
      businessOwner: input.businessOwner,
      technicalOwner: input.technicalOwner,
      downtimeExpected: input.downtimeExpected,
      estimatedDowntime: input.estimatedDowntime,
      deploymentNotes: input.deploymentNotes
    };
    this.store.update((list) => [release, ...list]);
    this.persist();
    return of(release);
  }

  private load(): Release[] {
    try {
      const raw = localStorage.getItem(KEY);
      return raw ? (JSON.parse(raw) as Release[]) : MOCK_RELEASES;
    } catch {
      return MOCK_RELEASES;
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
