namespace Gms.Api.Domain;

/// <summary>Append-only audit event for a notification.</summary>
public class NotificationEvent
{
    public Guid Id { get; set; }

    public Guid NotificationId { get; set; }
    public Notification? Notification { get; set; }

    public Guid ActorUserId { get; set; }

    /// <summary>See <see cref="Gms.Api.Common.NotificationEventTypes"/>.</summary>
    public string EventType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
