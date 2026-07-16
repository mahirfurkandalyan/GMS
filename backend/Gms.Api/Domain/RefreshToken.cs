namespace Gms.Api.Domain;

/// <summary>
/// A rotating refresh token. Only a cryptographic hash of the token is stored — the
/// raw value is returned to the client once and never persisted. A token is usable
/// only while it is neither expired nor revoked; on use it is revoked and replaced
/// (rotation), linking <see cref="ReplacedByTokenId"/>.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    /// <summary>SHA-256 hash of the raw token. NEVER exposed via DTOs or logs.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByIp { get; set; }

    public DateTime? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public string? ReasonRevoked { get; set; }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;
    public bool IsActive(DateTime utcNow) => RevokedAt is null && !IsExpired(utcNow);
}
