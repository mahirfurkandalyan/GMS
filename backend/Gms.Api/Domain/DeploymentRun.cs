using Gms.Api.Common;

namespace Gms.Api.Domain;

/// <summary>
/// A single execution (deployment run) of a scheduled ReleasePlan. Owns the
/// execution lifecycle and its ordered steps. Execution extends Release Planning:
/// a run always belongs to exactly one ReleasePlan and never duplicates release data.
/// </summary>
public class DeploymentRun
{
    public Guid Id { get; set; }

    public Guid ReleasePlanId { get; set; }
    public ReleasePlan? ReleasePlan { get; set; }

    /// <summary>Human-readable number, e.g. DEP-2026-000001.</summary>
    public string ExecutionNo { get; set; } = string.Empty;

    /// <summary>Created | Running | Completed | Failed | RolledBack.</summary>
    public string Status { get; set; } = string.Empty;

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Guid ExecutedByUserId { get; set; }
    public AppUser? ExecutedByUser { get; set; }

    /// <summary>Pending | Success | Failure | RolledBack.</summary>
    public string OverallResult { get; set; } = DeploymentResults.Pending;

    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    /// <summary>Optimistic concurrency token (reuses the hardening pattern).</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Relationships
    public ICollection<DeploymentStep> Steps { get; set; } = new List<DeploymentStep>();
    public ICollection<DeploymentEvent> Events { get; set; } = new List<DeploymentEvent>();

    /// <summary>Guarded status transition — the aggregate owns its own lifecycle.</summary>
    public void TransitionTo(string target)
    {
        StatusTransition.Ensure(DeploymentRunStatuses.Transitions, nameof(DeploymentRun), Status, target);
        Status = target;
    }
}
