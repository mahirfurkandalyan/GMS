namespace Gms.Api.Domain;

/// <summary>
/// One executable step of a DeploymentRun, materialised 1:1 from a ReleasePlanItem
/// and ordered by its DeploymentOrder. Step ordering (single active step, no skipping
/// ahead) is enforced by the ExecutionService — the same approach used for ApprovalStep.
/// </summary>
public class DeploymentStep
{
    public Guid Id { get; set; }

    public Guid DeploymentRunId { get; set; }
    public DeploymentRun? DeploymentRun { get; set; }

    public Guid ReleasePlanItemId { get; set; }
    public ReleasePlanItem? ReleasePlanItem { get; set; }

    public int StepOrder { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>Waiting | Running | Completed | Failed | Skipped | RolledBack.</summary>
    public string Status { get; set; } = string.Empty;

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Guid? ExecutedByUserId { get; set; }
    public AppUser? ExecutedByUser { get; set; }

    /// <summary>Pending | Success | Failure | RolledBack.</summary>
    public string ExecutionResult { get; set; } = string.Empty;

    public bool RollbackExecuted { get; set; }

    public string Notes { get; set; } = string.Empty;
}
