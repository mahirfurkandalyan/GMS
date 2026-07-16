/**
 * Frontend models for the Release Planning domain — DTO-aligned with the backend
 * (Gms.Api.Contracts.ReleasePlanDtos). Enum/string values are stored VERBATIM as the backend emits
 * them (PascalCase: releaseType 'Major', status 'Planned'/'Scheduled'). UI labels resolve separately
 * (see release-labels.ts). Never send a display label as a backend value.
 */

export interface ReleasePlanListItem {
  id: string;
  releaseNo: string;
  name: string;
  version: string;
  customerName: string;
  projectName: string;
  environmentName: string;
  releaseType: string;
  status: string;
  riskLevel: string;
  riskScore: number;
  changeCount: number;
  totalEstimatedMinutes: number;
  plannedDeploymentStart: string | null;
  releaseManagerName: string;
  createdAt: string;
}

export interface ReleasePlanItem {
  id: string;
  changeRequestId: string;
  changeNo: string;
  changeTitle: string;
  changeStatus: string;
  changeRiskLevel: string;
  deploymentOrder: number;
  estimatedMinutes: number;
  rollbackRequired: boolean;
}

export interface ReleaseDeploymentPlan {
  deploymentStrategy: string;
  communicationPlan: string;
  rollbackStrategy: string;
  downtimeExpected: boolean;
  estimatedDowntimeMinutes: number;
  notes: string;
}

export interface ReleaseDocumentReference {
  id: string;
  documentType: string;
  documentName: string;
  version: string;
  createdAt: string;
}

export interface ReleaseAuditEvent {
  id: string;
  eventType: string;
  description: string;
  actorUserId: string;
  /** Resolved actor display name (backend join); may be absent on older payloads. */
  actorUserName?: string;
  createdAt: string;
}

export interface ReleasePlanDetail {
  id: string;
  releaseNo: string;
  name: string;
  version: string;
  customerId: string;
  customerName: string;
  projectId: string;
  projectName: string;
  environmentId: string;
  environmentName: string;
  releaseType: string;
  status: string;
  riskLevel: string;
  riskScore: number;
  totalEstimatedMinutes: number;
  plannedDeploymentStart: string | null;
  plannedDeploymentEnd: string | null;
  rollbackWindow: string;
  businessOwner: string;
  technicalOwner: string;
  releaseManagerUserId: string;
  releaseManagerName: string;
  description: string;
  createdAt: string;
  updatedAt: string | null;
  /** Base64 optimistic-concurrency token — echo back on update to detect lost updates (409). */
  rowVersion: string;
  items: ReleasePlanItem[];
  deploymentPlan: ReleaseDeploymentPlan | null;
  documents: ReleaseDocumentReference[];
  auditEvents: ReleaseAuditEvent[];
}

/* ── Query / filter (only backend-supported filters) ─────── */

export interface ReleaseListQuery {
  customerId?: string;
  projectId?: string;
  environmentId?: string;
  status?: string;
  search?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
}

/* ── Write models (match backend Create/Update DTOs) ─────── */

export interface CreateReleaseDeploymentPlanInput {
  deploymentStrategy?: string;
  communicationPlan?: string;
  rollbackStrategy?: string;
  downtimeExpected: boolean;
  estimatedDowntimeMinutes: number;
  notes?: string;
}

export interface CreateReleaseDocumentInput {
  documentType: string;
  documentName: string;
  version?: string;
}

export interface CreateReleasePlanInput {
  name: string;
  version: string;
  customerId: string;
  projectId: string;
  environmentId: string;
  releaseType: string;
  plannedDeploymentStart?: string | null;
  plannedDeploymentEnd?: string | null;
  rollbackWindow?: string;
  businessOwner?: string;
  technicalOwner?: string;
  releaseManagerUserId: string;
  description?: string;
  changeIds: string[];
  deploymentPlan?: CreateReleaseDeploymentPlanInput;
  documents: CreateReleaseDocumentInput[];
  // Actor (creator) is resolved from the JWT server-side — never sent from the client.
}

export interface UpdateReleasePlanInput {
  name?: string;
  version?: string;
  releaseType?: string;
  plannedDeploymentStart?: string | null;
  plannedDeploymentEnd?: string | null;
  rollbackWindow?: string;
  businessOwner?: string;
  technicalOwner?: string;
  description?: string;
  deploymentPlan?: CreateReleaseDeploymentPlanInput;
  /** Base64 concurrency token loaded with the record; a mismatch yields 409. */
  rowVersion?: string;
}
