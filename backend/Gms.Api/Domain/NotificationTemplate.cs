namespace Gms.Api.Domain;

/// <summary>
/// A reusable notification template. Subject/Body support {{placeholder}} tokens rendered
/// at send time. Seeded system templates (IsSystem) drive all domain notifications.
/// </summary>
public class NotificationTemplate
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; }
}
