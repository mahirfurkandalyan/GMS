/**
 * Frontend models for the Workflow runtime — DTO-aligned with Gms.Api.Contracts.WorkflowDtos.
 * Enum/string values are stored verbatim (PascalCase: status 'Waiting'/'Completed', stepType
 * 'Approval'/'ManualTask', etc.). Labels resolve through the `workflows` Transloco scope.
 */

export interface WorkflowInstanceListItem {
  id: string;
  instanceNo: string;
  workflowDefinitionId: string;
  workflowName: string;
  triggerObjectType: string;
  triggerObjectId: string;
  triggerObjectNumber: string | null;
  status: string;
  currentStepName: string | null;
  createdAt: string;
  completedAt: string | null;
}

export interface WorkflowStepInstance {
  id: string;
  stepKey: string;
  name: string;
  stepType: string;
  stepOrder: number;
  status: string;
  assignedRole: string | null;
  assignedUserId: string | null;
  dueAt: string | null;
  actionedByUserId: string | null;
  result: string | null;
  comment: string | null;
  createdAt: string;
  activatedAt: string | null;
  completedAt: string | null;
}

export interface WorkflowEvent {
  id: string;
  workflowStepInstanceId: string | null;
  eventType: string;
  description: string;
  actorUserId: string;
  /** Resolved actor display name (backend join); may be absent on older payloads. */
  actorUserName?: string;
  createdAt: string;
}

export interface WorkflowInstanceDetail {
  id: string;
  instanceNo: string;
  workflowDefinitionId: string;
  workflowCode: string;
  workflowName: string;
  workflowVersionId: string;
  versionNumber: number;
  triggerObjectType: string;
  triggerObjectId: string;
  triggerObjectNumber: string | null;
  status: string;
  currentStepInstanceId: string | null;
  outcome: string | null;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
  /** Base64 concurrency token — echoed back on cancel. */
  rowVersion: string;
  steps: WorkflowStepInstance[];
  events: WorkflowEvent[];
}

/** A task (manual/approval step) awaiting the current user's action (GET tasks/mine). */
export interface WorkflowTaskItem {
  instanceId: string;
  instanceNo: string;
  stepInstanceId: string;
  stepKey: string;
  stepName: string;
  stepType: string;
  workflowName: string;
  triggerObjectType: string;
  triggerObjectId: string;
  triggerObjectNumber: string | null;
  assignedRole: string | null;
  assignedUserId: string | null;
  dueAt: string | null;
  isOverdue: boolean;
  createdAt: string;
}

export interface WorkflowInstanceQuery {
  definitionId?: string;
  status?: string;
  triggerObjectId?: string;
  search?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
}
