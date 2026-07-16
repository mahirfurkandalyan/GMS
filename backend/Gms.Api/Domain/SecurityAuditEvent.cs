namespace Gms.Api.Domain;

/// <summary>
/// Append-only security audit event (login, lockout, token refresh, logout, password
/// change, etc.). Never stores raw tokens or passwords in the description.
/// </summary>
public class SecurityAuditEvent
{
    public Guid Id { get; set; }

    /// <summary>Nullable — a failed login for an unknown email has no user id.</summary>
    public Guid? UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>See <see cref="Gms.Api.Common.SecurityEventTypes"/>.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>"Success" | "Failure".</summary>
    public string Result { get; set; } = string.Empty;

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
