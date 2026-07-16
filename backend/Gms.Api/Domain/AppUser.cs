namespace Gms.Api.Domain;

/// <summary>
/// System user with real credentials and RBAC. Passwords are stored only as a hash
/// (PasswordHasher output). Email is normalized (upper-invariant) and unique.
/// </summary>
public class AppUser
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>Upper-invariant email used for unique lookups. Never expose in DTOs.</summary>
    public string NormalizedEmail { get; set; } = string.Empty;

    /// <summary>PasswordHasher output. NEVER exposed via DTOs or logs.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Textual status (kept for backward compatibility): "Active" | "Passive".</summary>
    public string Status { get; set; } = "Active";

    /// <summary>Authoritative activation flag used by authentication.</summary>
    public bool IsActive { get; set; } = true;

    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Relationships
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
