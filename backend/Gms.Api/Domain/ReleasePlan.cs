using Gms.Api.Common;

namespace Gms.Api.Domain;

/// <summary>
/// A release plan — the System of Record for deployment planning. A release is
/// ALWAYS composed of approved changes and carries a complete deployment plan.
/// Validation / Execution / Deployment / Calendar / Reports depend on this entity.
/// </summary>
public class ReleasePlan
{
    public Guid Id { get; set; }

    /// <summary>Human-readable number, e.g. REL-2026-000001.</summary>
    public string ReleaseNo { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid EnvironmentId { get; set; }
    public AppEnvironment? Environment { get; set; }

    /// <summary>Major | Minor | Patch | Hotfix | Emergency.</summary>
    public string ReleaseType { get; set; } = string.Empty;

    /// <summary>Draft | Planned | Scheduled | InProgress | Completed | Cancelled.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Auto-calculated release risk (never chosen manually).</summary>
    public string RiskLevel { get; set; } = string.Empty;
    public int RiskScore { get; set; }

    /// <summary>Sum of item estimated minutes.</summary>
    public int TotalEstimatedMinutes { get; set; }

    public DateTime? PlannedDeploymentStart { get; set; }
    public DateTime? PlannedDeploymentEnd { get; set; }
    public string RollbackWindow { get; set; } = string.Empty;

    public string BusinessOwner { get; set; } = string.Empty;
    public string TechnicalOwner { get; set; } = string.Empty;

    public Guid ReleaseManagerUserId { get; set; }
    public AppUser? ReleaseManagerUser { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Relationships
    public ReleaseDeploymentPlan? DeploymentPlan { get; set; }
    public ICollection<ReleasePlanItem> Items { get; set; } = new List<ReleasePlanItem>();
    public ICollection<ReleaseDocument> Documents { get; set; } = new List<ReleaseDocument>();
    public ICollection<ReleaseAuditEvent> AuditEvents { get; set; } = new List<ReleaseAuditEvent>();

    /// <summary>Guarded status transition — the aggregate owns its own lifecycle.</summary>
    public void TransitionTo(string target)
    {
        StatusTransition.Ensure(ReleaseStatuses.Transitions, nameof(ReleasePlan), Status, target);
        Status = target;
    }
}
