namespace Gms.Api.Contracts;

/// <summary>Summary row for the notifications list.</summary>
public class NotificationListDto
{
    public Guid Id { get; set; }
    public string NotificationNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RecipientRole { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Full notification detail: deliveries + audit events.</summary>
public class NotificationDetailDto
{
    public Guid Id { get; set; }
    public string NotificationNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid RecipientUserId { get; set; }
    public string? RecipientRole { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;

    public List<NotificationDeliveryDto> Deliveries { get; set; } = new();
    public List<NotificationEventDto> Events { get; set; } = new();
}

public class NotificationDeliveryDto
{
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? SentAt { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
}

public class NotificationEventDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationTemplateDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
}

public class NotificationPreferenceDto
{
    public string Module { get; set; } = string.Empty;
    public bool InAppEnabled { get; set; }
    public bool EmailEnabled { get; set; }
}

/* ── request DTOs ── */

public class UpdatePreferencesDto
{
    public List<NotificationPreferenceDto> Preferences { get; set; } = new();
}

public class BroadcastNotificationDto
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Information";

    /// <summary>Target role; null/empty = all active users.</summary>
    public string? Role { get; set; }
}

public class UpdateTemplateDto
{
    public string? Name { get; set; }
    public string? SubjectTemplate { get; set; }
    public string? BodyTemplate { get; set; }
}
