namespace Gms.Api.Contracts;

/// <summary>Summary row for the deployments list.</summary>
public class DeploymentRunListDto
{
    public Guid Id { get; set; }
    public string ExecutionNo { get; set; } = string.Empty;
    public Guid ReleasePlanId { get; set; }
    public string ReleaseNo { get; set; } = string.Empty;
    public string ReleaseName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OverallResult { get; set; } = string.Empty;
    public int StepCount { get; set; }
    public int CompletedStepCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string ExecutedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Full deployment run detail, including ordered steps and audit events.</summary>
public class DeploymentRunDetailDto
{
    public Guid Id { get; set; }
    public string ExecutionNo { get; set; } = string.Empty;
    public Guid ReleasePlanId { get; set; }
    public string ReleaseNo { get; set; } = string.Empty;
    public string ReleaseName { get; set; } = string.Empty;
    public string ReleaseStatus { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OverallResult { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid ExecutedByUserId { get; set; }
    public string ExecutedByUserName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>Base64 concurrency token; echo it back on actions to detect conflicts (409).</summary>
    public string RowVersion { get; set; } = string.Empty;

    public List<DeploymentStepDto> Steps { get; set; } = new();
    public List<DeploymentEventDto> Events { get; set; } = new();
}

/// <summary>One executable step of a deployment run.</summary>
public class DeploymentStepDto
{
    public Guid Id { get; set; }
    public Guid ReleasePlanItemId { get; set; }
    public int StepOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ExecutionResult { get; set; } = string.Empty;
    public bool RollbackExecuted { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? ExecutedByUserId { get; set; }
    public string? ExecutedByUserName { get; set; }
    public string Notes { get; set; } = string.Empty;

    // Linked change (read-only context from the release plan item)
    public Guid ChangeRequestId { get; set; }
    public string ChangeNo { get; set; } = string.Empty;
    public string ChangeTitle { get; set; } = string.Empty;
}

/// <summary>Append-only deployment audit event.</summary>
public class DeploymentEventDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Request body to create a deployment run for a scheduled release.</summary>
public class CreateDeploymentRunDto
{
    public Guid ReleasePlanId { get; set; }
    // Actor comes from the authenticated context, not the request body.
    public string? Notes { get; set; }
}

/// <summary>
/// Request body for run/step actions (start, start-next-step, complete-step,
/// fail-step, rollback). Notes doubles as the failure reason on fail-step.
/// </summary>
public class DeploymentActionDto
{
    // Actor comes from the authenticated context, not the request body.
    public string? Notes { get; set; }

    /// <summary>Optional base64 concurrency token; a mismatch yields 409 Conflict.</summary>
    public string? RowVersion { get; set; }
}
