namespace Gms.Api.Domain;

/// <summary>Append-only audit event for an approval request.</summary>
public class ApprovalAuditEvent
{
    public Guid Id { get; set; }

    public Guid ApprovalRequestId { get; set; }
    public ApprovalRequest? ApprovalRequest { get; set; }

    /// <summary>ApprovalCreated | StepActivated | Approved | Rejected | RevisionRequested | ApprovalCompleted | ApprovalCancelled.</summary>
    public string EventType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid ActorUserId { get; set; }
    public AppUser? ActorUser { get; set; }

    public DateTime CreatedAt { get; set; }
}
