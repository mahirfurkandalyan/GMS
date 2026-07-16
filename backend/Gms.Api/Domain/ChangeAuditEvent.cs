namespace Gms.Api.Domain;

/// <summary>
/// An append-only audit event for a change request. Written by the API on every
/// meaningful action (created, updated, submitted, cancelled, revision created).
/// </summary>
public class ChangeAuditEvent
{
    public Guid Id { get; set; }

    public Guid ChangeRequestId { get; set; }
    public ChangeRequest? ChangeRequest { get; set; }

    /// <summary>ChangeCreated | ChangeUpdated | ChangeSubmitted | ChangeCancelled | RevisionCreated.</summary>
    public string EventType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Actor who triggered the event (stored as id; no navigation to avoid cascade paths).</summary>
    public Guid ActorUserId { get; set; }

    public DateTime CreatedAt { get; set; }
}
