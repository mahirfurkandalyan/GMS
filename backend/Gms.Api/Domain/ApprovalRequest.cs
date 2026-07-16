using Gms.Api.Common;

namespace Gms.Api.Domain;

/// <summary>
/// An approval request for a governance object (currently a ChangeRequest).
/// Owns an ordered chain of steps, the decisions made, and its own audit trail.
/// </summary>
public class ApprovalRequest
{
    public Guid Id { get; set; }

    /// <summary>Human-readable number, e.g. APR-2026-000001.</summary>
    public string ApprovalNo { get; set; } = string.Empty;

    /// <summary>Target object type (e.g. "ChangeRequest").</summary>
    public string RelatedObjectType { get; set; } = string.Empty;
    public Guid RelatedObjectId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Draft | Pending | InProgress | Approved | Rejected | Cancelled | Expired.</summary>
    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public Guid RequestedByUserId { get; set; }
    public AppUser? RequestedByUser { get; set; }

    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Relationships
    public ICollection<ApprovalStep> Steps { get; set; } = new List<ApprovalStep>();
    public ICollection<ApprovalDecision> Decisions { get; set; } = new List<ApprovalDecision>();
    public ICollection<ApprovalAuditEvent> AuditEvents { get; set; } = new List<ApprovalAuditEvent>();

    /// <summary>Guarded status transition — the aggregate owns its own lifecycle.</summary>
    public void TransitionTo(string target)
    {
        StatusTransition.Ensure(ApprovalStatuses.Transitions, nameof(ApprovalRequest), Status, target);
        Status = target;
    }
}
