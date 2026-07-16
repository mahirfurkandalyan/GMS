namespace Gms.Api.Domain;

/// <summary>
/// Append-only audit event for a workflow instance (started, step activated/completed/rejected,
/// condition evaluated, notification fired, completed/failed/cancelled/paused/resumed). Feeds
/// the unified audit read model under the WORKFLOW module. Never mutated after creation.
/// </summary>
public class WorkflowEvent
{
    public Guid Id { get; set; }

    public Guid WorkflowInstanceId { get; set; }
    public WorkflowInstance? WorkflowInstance { get; set; }

    /// <summary>Optional step this event relates to (null for instance-level events).</summary>
    public Guid? WorkflowStepInstanceId { get; set; }

    public Guid ActorUserId { get; set; }

    /// <summary>See <see cref="Gms.Api.Common.WorkflowEventTypes"/>.</summary>
    public string EventType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
