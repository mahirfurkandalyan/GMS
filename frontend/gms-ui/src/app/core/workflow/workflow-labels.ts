/**
 * Workflow runtime enum value → i18n key helpers (resolve under the `workflows` Transloco scope).
 * Values are exact backend strings (WorkflowInstanceStatuses / WorkflowStepStatuses / WorkflowStepTypes).
 */
export const workflowStatusLabelKey = (v: string) => `workflows.status.${v}`;
export const workflowStepStatusLabelKey = (v: string) => `workflows.stepStatus.${v}`;
export const workflowStepTypeLabelKey = (v: string) => `workflows.stepType.${v}`;
export const workflowEventLabelKey = (v: string) => `workflows.event.${v}`;
