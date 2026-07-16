namespace Gms.Api.Domain;

/// <summary>
/// Tracks dispatch of a notification over a channel (InApp, Email, …). Extensible for
/// future channels (SMS/Teams/Slack/WebHook). Records retry count and failure reason.
/// </summary>
public class NotificationDelivery
{
    public Guid Id { get; set; }

    public Guid NotificationId { get; set; }
    public Notification? Notification { get; set; }

    /// <summary>InApp | Email | (future) SMS/Teams/Slack/WebHook.</summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>Pending | Processing | Sent | Delivered | Failed | DeadLetter.</summary>
    public string Status { get; set; } = string.Empty;

    public DateTime? SentAt { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }

    /// <summary>Total attempts made (increments each processing pass).</summary>
    public int AttemptCount { get; set; }

    /// <summary>Earliest time a retryable (Failed) delivery may be re-attempted (backoff window).</summary>
    public DateTime? NextAttemptAt { get; set; }

    public DateTime? LastAttemptAt { get; set; }

    /// <summary>Lease expiry — while in the future the row is claimed and other workers skip it.</summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>Node instance that currently holds the lease.</summary>
    public string? LockedBy { get; set; }

    /// <summary>Optimistic concurrency token (claim races resolve via this).</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
