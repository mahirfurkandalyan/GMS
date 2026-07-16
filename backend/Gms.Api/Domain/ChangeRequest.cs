using Gms.Api.Common;

namespace Gms.Api.Domain;

/// <summary>
/// A governance change request — the origin of every downstream process
/// (approval, validation, execution, release, audit). Single source of truth.
/// </summary>
public class ChangeRequest
{
    public Guid Id { get; set; }

    /// <summary>Human-readable number, e.g. CHG-2026-000001.</summary>
    public string ChangeNo { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BusinessReason { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid EnvironmentId { get; set; }
    public AppEnvironment? Environment { get; set; }

    /// <summary>Standard | Normal | Emergency.</summary>
    public string ChangeClass { get; set; } = string.Empty;

    /// <summary>Technical type (ApplicationDeployment, SqlDataFix, ...).</summary>
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>Low | Medium | High | Critical.</summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>Draft | Submitted | UnderReview | ... | Completed | Cancelled.</summary>
    public string Status { get; set; } = "Draft";

    /// <summary>Auto-calculated risk level (never chosen manually).</summary>
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>Auto-calculated numeric risk score (0..).</summary>
    public int RiskScore { get; set; }

    public DateTime? PlannedImplementationDate { get; set; }
    public DateTime? PlannedRollbackDate { get; set; }

    public string? SourceSystem { get; set; }
    public string? SourceReference { get; set; }

    public Guid CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Relationships
    public ICollection<ChangeRevision> Revisions { get; set; } = new List<ChangeRevision>();
    public ICollection<ChangeAffectedAsset> Assets { get; set; } = new List<ChangeAffectedAsset>();
    public ICollection<ChangeDocument> Documents { get; set; } = new List<ChangeDocument>();
    public ICollection<ChangeAuditEvent> AuditEvents { get; set; } = new List<ChangeAuditEvent>();

    /// <summary>Guarded status transition — the aggregate owns its own lifecycle.</summary>
    public void TransitionTo(string target)
    {
        StatusTransition.Ensure(ChangeStatuses.Transitions, nameof(ChangeRequest), Status, target);
        Status = target;
    }
}
