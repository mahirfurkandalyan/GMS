namespace Gms.Api.Domain;

/// <summary>
/// The runtime state of one step within a workflow instance. Automatic steps are created and
/// immediately Completed by the engine; manual steps (ManualTask/Approval) are created Active
/// and pause the instance until a human completes or rejects them. Carries the resolved
/// assignee (from the step definition's role/user rule) and the recorded result.
/// </summary>
public class WorkflowStepInstance
{
    public Guid Id { get; set; }

    public Guid WorkflowInstanceId { get; set; }
    public WorkflowInstance? WorkflowInstance { get; set; }

    /// <summary>The step definition this instance realises (immutable version graph).</summary>
    public Guid StepDefinitionId { get; set; }

    /// <summary>Denormalised step key/name/type for display and audit (snapshot).</summary>
    public string StepKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;
    public int StepOrder { get; set; }

    /// <summary>Waiting | Active | Completed | Rejected | Skipped | Failed | Cancelled (see <see cref="Gms.Api.Common.WorkflowStepStatuses"/>).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>For manual steps: resolved role assignment (snapshot of the definition).</summary>
    public string? AssignedRole { get; set; }

    /// <summary>For manual steps: resolved user assignment.</summary>
    public Guid? AssignedUserId { get; set; }

    /// <summary>SLA deadline (set on activation when the step defines DueInHours).</summary>
    public DateTime? DueAt { get; set; }

    /// <summary>User who actioned a manual step (null for automatic/unactioned).</summary>
    public Guid? ActionedByUserId { get; set; }

    /// <summary>Result vocabulary: Approved | Rejected | Completed | Auto (see <see cref="Gms.Api.Common.WorkflowStepResults"/>).</summary>
    public string? Result { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Last time a "due soon" reminder was generated (cooldown/dedup for the SLA worker).</summary>
    public DateTime? DueSoonNotifiedAt { get; set; }

    /// <summary>Last time an "overdue" reminder was generated (cooldown/dedup for the SLA worker).</summary>
    public DateTime? OverdueNotifiedAt { get; set; }
}
