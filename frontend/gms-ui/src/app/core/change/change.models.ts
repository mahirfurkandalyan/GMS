/**
 * Frontend models for the Change Management domain — DTO-aligned with the backend
 * (Gms.Api.Contracts.ChangeRequestDtos). String/enum values are stored VERBATIM as the backend
 * emits them (PascalCase, e.g. changeType 'DatabaseSchemaChange', status 'UnderReview'); UI labels
 * are resolved separately (see change-labels.ts). Never use a display label as a backend value.
 */

/* ── Read models ─────────────────────────────────────────── */

export interface ChangeRequestListItem {
  id: string;
  changeNo: string;
  title: string;
  customerName: string;
  projectName: string;
  environmentName: string;
  changeClass: string;
  changeType: string;
  priority: string;
  status: string;
  riskLevel: string;
  riskScore: number;
  plannedImplementationDate: string | null;
  createdByUserName: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface ChangeReadinessFinding {
  code: string;
  severity: string; // 'Critical' | 'Warning'
  message: string;
  recommendation: string;
}

export interface ChangeReadiness {
  readinessScore: number;
  findings: ChangeReadinessFinding[];
}

export interface ChangeRevision {
  id: string;
  revisionNo: number;
  technicalSummary: string;
  implementationNotes: string;
  deploymentInstructions: string;
  sqlScript: string;
  rollbackScript: string;
  rollbackStrategy: string;
  rollbackOwner: string;
  estimatedDurationMinutes: number;
  createdByUserId: string;
  createdAt: string;
}

export interface ChangeAffectedAsset {
  id: string;
  assetType: string;
  assetName: string;
  criticality: string;
  description: string;
}

export interface ChangeDocumentReference {
  id: string;
  documentType: string;
  documentName: string;
  version: string;
  status: string;
  createdAt: string;
}

export interface ChangeAuditEvent {
  id: string;
  eventType: string;
  description: string;
  actorUserId: string;
  /** Resolved actor display name (backend join); may be absent on older payloads. */
  actorUserName?: string;
  createdAt: string;
}

export interface ChangeRequestDetail extends ChangeRequestListItem {
  description: string;
  businessReason: string;
  customerId: string;
  projectId: string;
  environmentId: string;
  plannedRollbackDate: string | null;
  sourceSystem: string | null;
  sourceReference: string | null;
  createdByUserId: string;
  /** Base64 optimistic-concurrency token — echo back on update to detect lost updates (409). */
  rowVersion: string;
  latestRevision: ChangeRevision | null;
  assets: ChangeAffectedAsset[];
  documents: ChangeDocumentReference[];
  auditEvents: ChangeAuditEvent[];
  readiness: ChangeReadiness;
}

/* ── Query / filter ──────────────────────────────────────── */

export interface ChangeListQuery {
  customerId?: string;
  projectId?: string;
  environmentId?: string;
  status?: string;
  changeClass?: string;
  changeType?: string;
  riskLevel?: string;
  search?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
}

/* ── Write models (match backend Create/Update DTOs) ─────── */

export interface CreateChangeRevisionInput {
  technicalSummary?: string;
  implementationNotes?: string;
  deploymentInstructions?: string;
  sqlScript?: string;
  rollbackScript?: string;
  rollbackStrategy?: string;
  rollbackOwner?: string;
  estimatedDurationMinutes?: number;
}

export interface CreateChangeAssetInput {
  assetType: string;
  assetName: string;
  criticality: string;
  description?: string;
}

export interface CreateChangeDocumentInput {
  documentType: string;
  documentName: string;
  version?: string;
  status?: string;
}

export interface CreateChangeRequestInput {
  title: string;
  description?: string;
  businessReason?: string;
  customerId: string;
  projectId: string;
  environmentId: string;
  changeClass: string;
  changeType: string;
  priority: string;
  plannedImplementationDate?: string | null;
  plannedRollbackDate?: string | null;
  sourceSystem?: string;
  sourceReference?: string;
  revision: CreateChangeRevisionInput;
  assets: CreateChangeAssetInput[];
  documents: CreateChangeDocumentInput[];
  // Actor (creator) is resolved from the JWT on the backend — never sent from the client.
}

export interface UpdateChangeRequestInput {
  title?: string;
  description?: string;
  businessReason?: string;
  environmentId?: string;
  changeClass?: string;
  changeType?: string;
  priority?: string;
  plannedImplementationDate?: string | null;
  plannedRollbackDate?: string | null;
  sourceSystem?: string;
  sourceReference?: string;
  /** Base64 concurrency token loaded with the record; a mismatch yields 409. */
  rowVersion?: string;
}
