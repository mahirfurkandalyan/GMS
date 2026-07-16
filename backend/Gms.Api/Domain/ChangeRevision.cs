namespace Gms.Api.Domain;

/// <summary>
/// An immutable technical revision of a change request. Each revision captures
/// the deployment/rollback plan at a point in time. RevisionNo increments.
/// </summary>
public class ChangeRevision
{
    public Guid Id { get; set; }

    public Guid ChangeRequestId { get; set; }
    public ChangeRequest? ChangeRequest { get; set; }

    public int RevisionNo { get; set; }

    public string TechnicalSummary { get; set; } = string.Empty;
    public string ImplementationNotes { get; set; } = string.Empty;
    public string DeploymentInstructions { get; set; } = string.Empty;
    public string SqlScript { get; set; } = string.Empty;
    public string RollbackScript { get; set; } = string.Empty;
    public string RollbackStrategy { get; set; } = string.Empty;
    public string RollbackOwner { get; set; } = string.Empty;

    public int EstimatedDurationMinutes { get; set; }

    /// <summary>Actor who created the revision (stored as id; no navigation to avoid cascade paths).</summary>
    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }
}
