namespace Gms.Api.Contracts;

/* ── Definition read DTOs ─────────────────────────────── */

/// <summary>Workflow definition summary (list rows).</summary>
public class WorkflowDefinitionListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TriggerObjectType { get; set; } = string.Empty;
    public string TriggerEvent { get; set; } = string.Empty;
    public string? ChangeClass { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ActiveVersionId { get; set; }
    public int? ActiveVersionNumber { get; set; }
    public int VersionCount { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Full workflow definition detail incl. all versions.</summary>
public class WorkflowDefinitionDetailDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TriggerObjectType { get; set; } = string.Empty;
    public string TriggerEvent { get; set; } = string.Empty;
    public string? ChangeClass { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ActiveVersionId { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public List<WorkflowVersionDto> Versions { get; set; } = new();
}

/// <summary>A workflow version with its step/transition graph.</summary>
public class WorkflowVersionDto
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? StartStepKey { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public List<WorkflowStepDto> Steps { get; set; } = new();
    public List<WorkflowTransitionDto> Transitions { get; set; } = new();
}

public class WorkflowStepDto
{
    public Guid Id { get; set; }
    public string StepKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string? AssignedRole { get; set; }
    public Guid? AssignedUserId { get; set; }
    public bool IsRequired { get; set; }
    public int? DueInHours { get; set; }
    public string? NotificationTemplateCode { get; set; }
    public string? NotificationRole { get; set; }
    public string? Description { get; set; }
}

public class WorkflowTransitionDto
{
    public Guid Id { get; set; }
    public string FromStepKey { get; set; } = string.Empty;
    public string ToStepKey { get; set; } = string.Empty;
    public string ConditionType { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string? ConditionField { get; set; }
    public string? Operator { get; set; }
    public string? ExpectedValue { get; set; }
    public string? Description { get; set; }
}

/* ── Definition write DTOs ────────────────────────────── */

/// <summary>Create a new workflow definition together with its first Draft version graph.</summary>
public class CreateWorkflowDefinitionDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string TriggerObjectType { get; set; } = string.Empty;
    public string TriggerEvent { get; set; } = string.Empty;
    public string? ChangeClass { get; set; }
    public List<CreateWorkflowStepDto> Steps { get; set; } = new();
    public List<CreateWorkflowTransitionDto> Transitions { get; set; } = new();
}

/// <summary>Replace the step/transition graph of an existing Draft version.</summary>
public class UpdateWorkflowVersionDto
{
    public string? Notes { get; set; }
    public List<CreateWorkflowStepDto> Steps { get; set; } = new();
    public List<CreateWorkflowTransitionDto> Transitions { get; set; } = new();
}

public class CreateWorkflowStepDto
{
    public string StepKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string? AssignedRole { get; set; }
    public Guid? AssignedUserId { get; set; }
    public bool IsRequired { get; set; } = true;
    public int? DueInHours { get; set; }
    public string? NotificationTemplateCode { get; set; }
    public string? NotificationRole { get; set; }
    public string? Description { get; set; }
}

public class CreateWorkflowTransitionDto
{
    public string FromStepKey { get; set; } = string.Empty;
    public string ToStepKey { get; set; } = string.Empty;
    public string ConditionType { get; set; } = "Always";
    public int Priority { get; set; } = 1;
    public string? ConditionField { get; set; }
    public string? Operator { get; set; }
    public string? ExpectedValue { get; set; }
    public string? Description { get; set; }
}

/// <summary>Validation result for a workflow version (errors block publish).</summary>
public class WorkflowValidationResultDto
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/* ── Instance / task DTOs ─────────────────────────────── */

public class WorkflowInstanceListDto
{
    public Guid Id { get; set; }
    public string InstanceNo { get; set; } = string.Empty;
    public Guid WorkflowDefinitionId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public string TriggerObjectType { get; set; } = string.Empty;
    public Guid TriggerObjectId { get; set; }
    public string? TriggerObjectNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CurrentStepName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class WorkflowInstanceDetailDto
{
    public Guid Id { get; set; }
    public string InstanceNo { get; set; } = string.Empty;
    public Guid WorkflowDefinitionId { get; set; }
    public string WorkflowCode { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public Guid WorkflowVersionId { get; set; }
    public int VersionNumber { get; set; }
    public string TriggerObjectType { get; set; } = string.Empty;
    public Guid TriggerObjectId { get; set; }
    public string? TriggerObjectNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? CurrentStepInstanceId { get; set; }
    public string? Outcome { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public List<WorkflowStepInstanceDto> Steps { get; set; } = new();
    public List<WorkflowEventDto> Events { get; set; } = new();
}

public class WorkflowStepInstanceDto
{
    public Guid Id { get; set; }
    public string StepKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AssignedRole { get; set; }
    public Guid? AssignedUserId { get; set; }
    public DateTime? DueAt { get; set; }
    public Guid? ActionedByUserId { get; set; }
    public string? Result { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class WorkflowEventDto
{
    public Guid Id { get; set; }
    public Guid? WorkflowStepInstanceId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    /// <summary>Resolved actor display name (joined from Users) for the timeline.</summary>
    public string ActorUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>A task (manual/approval step) awaiting the current user's action.</summary>
public class WorkflowTaskDto
{
    public Guid InstanceId { get; set; }
    public string InstanceNo { get; set; } = string.Empty;
    public Guid StepInstanceId { get; set; }
    public string StepKey { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public string TriggerObjectType { get; set; } = string.Empty;
    public Guid TriggerObjectId { get; set; }
    public string? TriggerObjectNumber { get; set; }
    public string? AssignedRole { get; set; }
    public Guid? AssignedUserId { get; set; }
    public DateTime? DueAt { get; set; }
    public bool IsOverdue { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Payload for completing/rejecting a workflow task.</summary>
public class WorkflowTaskActionDto
{
    public string? Comment { get; set; }
}

/// <summary>Payload for cancelling a running instance.</summary>
public class WorkflowCancelDto
{
    public string? Reason { get; set; }
    public string? RowVersion { get; set; }
}
