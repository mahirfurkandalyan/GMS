using Gms.Api.Common;

namespace Gms.Api.Domain;

/// <summary>
/// A validation run that provides evidence a completed DeploymentRun works as intended.
/// Validation extends Execution: a run always belongs to exactly one (Completed)
/// DeploymentRun and never duplicates execution/release logic. A passing run marks the
/// ReleasePlan as Accepted.
/// </summary>
public class ValidationRun
{
    public Guid Id { get; set; }

    public Guid DeploymentRunId { get; set; }
    public DeploymentRun? DeploymentRun { get; set; }

    /// <summary>Human-readable number, e.g. VAL-2026-000001.</summary>
    public string ValidationNo { get; set; } = string.Empty;

    /// <summary>Created | Running | Passed | Failed | Cancelled.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Functional | Smoke | Regression | UAT | Performance.</summary>
    public string ValidationType { get; set; } = string.Empty;

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Guid ValidatedByUserId { get; set; }
    public AppUser? ValidatedByUser { get; set; }

    /// <summary>Pending | Passed | Failed.</summary>
    public string OverallResult { get; set; } = ValidationResults.Pending;

    public string Summary { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    /// <summary>Optimistic concurrency token (reuses the hardening pattern).</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Relationships
    public ICollection<ValidationCheck> Checks { get; set; } = new List<ValidationCheck>();
    public ICollection<ValidationEvidence> Evidence { get; set; } = new List<ValidationEvidence>();
    public ICollection<ValidationEvent> Events { get; set; } = new List<ValidationEvent>();

    /// <summary>Guarded status transition — the aggregate owns its own lifecycle.</summary>
    public void TransitionTo(string target)
    {
        StatusTransition.Ensure(ValidationRunStatuses.Transitions, nameof(ValidationRun), Status, target);
        Status = target;
    }
}
