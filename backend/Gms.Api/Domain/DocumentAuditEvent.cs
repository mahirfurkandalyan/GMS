namespace Gms.Api.Domain;

/// <summary>Append-only audit event for a document.</summary>
public class DocumentAuditEvent
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }

    public Guid ActorUserId { get; set; }

    /// <summary>See <see cref="Gms.Api.Common.DocumentEventTypes"/>.</summary>
    public string EventType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
