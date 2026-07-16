using Gms.Api.Common;

namespace Gms.Api.Domain;

/// <summary>
/// A running execution of a specific workflow version against one governance object (the first
/// integration target is a ChangeRequest). The instance is the aggregate root of the runtime
/// side: it owns its step instances and events, tracks the current step, and holds a snapshot
/// of the trigger object's context fields used for condition evaluation. Runtime never mutates
/// the (immutable) definition — it only reads it.
/// </summary>
public class WorkflowInstance
{
    public Guid Id { get; set; }

    /// <summary>Human-readable number, e.g. WFI-2026-000001.</summary>
    public string InstanceNo { get; set; } = string.Empty;

    public Guid WorkflowDefinitionId { get; set; }
    public WorkflowDefinition? WorkflowDefinition { get; set; }

    public Guid WorkflowVersionId { get; set; }
    public WorkflowVersion? WorkflowVersion { get; set; }

    /// <summary>Trigger object type (e.g. ChangeRequest).</summary>
    public string TriggerObjectType { get; set; } = string.Empty;

    /// <summary>Trigger object id (e.g. the ChangeRequest id).</summary>
    public Guid TriggerObjectId { get; set; }

    /// <summary>Denormalised object number for display/audit (e.g. CHG-2026-000001).</summary>
    public string? TriggerObjectNumber { get; set; }

    /// <summary>Created | Running | Waiting | Completed | Failed | Cancelled (see <see cref="WorkflowInstanceStatuses"/>).</summary>
    public string Status { get; set; } = WorkflowInstanceStatuses.Created;

    /// <summary>The step the instance is currently at (null before start / after completion).</summary>
    public Guid? CurrentStepInstanceId { get; set; }

    /// <summary>Project context (for unified-audit joins / filtering); optional.</summary>
    public Guid? RelatedProjectId { get; set; }

    /// <summary>Environment context (for unified-audit joins / filtering); optional.</summary>
    public Guid? RelatedEnvironmentId { get; set; }

    /// <summary>JSON snapshot of the trigger object's allowlisted context fields (condition input).</summary>
    public string? ContextJson { get; set; }

    /// <summary>Final outcome summary (e.g. reject reason) once terminal.</summary>
    public string? Outcome { get; set; }

    public Guid StartedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Relationships
    public ICollection<WorkflowStepInstance> StepInstances { get; set; } = new List<WorkflowStepInstance>();
    public ICollection<WorkflowEvent> Events { get; set; } = new List<WorkflowEvent>();

    /// <summary>Guarded status transition — the aggregate owns its own lifecycle.</summary>
    public void TransitionTo(string target)
    {
        StatusTransition.Ensure(WorkflowInstanceStatuses.Transitions, nameof(WorkflowInstance), Status, target);
        Status = target;
    }
}
