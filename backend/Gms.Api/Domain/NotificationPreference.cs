namespace Gms.Api.Domain;

/// <summary>
/// A user's per-module channel preferences. When no row exists, defaults apply
/// (in-app on, email on). A unique index on (UserId, Module) prevents duplicates.
/// </summary>
public class NotificationPreference
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public string Module { get; set; } = string.Empty;

    public bool InAppEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
