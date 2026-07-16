using Gms.Api.Common;

namespace Gms.Api.Domain;

/// <summary>
/// A single in-app notification for one recipient — the aggregate root of the central
/// Notification Engine. Role/broadcast notifications are fanned out to one row per user so
/// read/unread state and delivery are per-user. No business domain creates notifications
/// directly; they all go through NotificationService.
/// </summary>
public class Notification
{
    public Guid Id { get; set; }

    /// <summary>Human-readable number, e.g. NTF-2026-000001.</summary>
    public string NotificationNo { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>Notification type — usually a template code (see <see cref="NotificationTemplates"/>).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Information | Success | Warning | Error | Critical.</summary>
    public string Severity { get; set; } = NotificationSeverities.Information;

    /// <summary>Owning module (for per-module preferences/filtering).</summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>Unread | Read | Archived | Deleted.</summary>
    public string Status { get; set; } = NotificationStatuses.Unread;

    public Guid RecipientUserId { get; set; }
    public AppUser? RecipientUser { get; set; }

    /// <summary>Role that triggered a role/broadcast notification (context only; null for direct).</summary>
    public string? RecipientRole { get; set; }

    /// <summary>Actor who caused the notification; null for system-generated.</summary>
    public Guid? CreatedByUserId { get; set; }

    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Relationships
    public ICollection<NotificationEvent> Events { get; set; } = new List<NotificationEvent>();
    public ICollection<NotificationDelivery> Deliveries { get; set; } = new List<NotificationDelivery>();

    /// <summary>Guarded status transition — the aggregate owns its own lifecycle.</summary>
    public void TransitionTo(string target)
    {
        StatusTransition.Ensure(NotificationStatuses.Transitions, nameof(Notification), Status, target);
        Status = target;
    }
}
