namespace Gms.Api.Domain;

/// <summary>Append-only audit event for a release plan.</summary>
public class ReleaseAuditEvent
{
    public Guid Id { get; set; }

    public Guid ReleasePlanId { get; set; }
    public ReleasePlan? ReleasePlan { get; set; }

    /// <summary>ReleaseCreated | ReleaseUpdated | ReleaseScheduled | ReleaseCompleted | ReleaseCancelled.</summary>
    public string EventType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid ActorUserId { get; set; }
    public AppUser? ActorUser { get; set; }

    public DateTime CreatedAt { get; set; }
}
