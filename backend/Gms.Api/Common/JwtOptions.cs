namespace Gms.Api.Common;

/// <summary>Bound from the "Jwt" configuration section.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "GmsApi";
    public string Audience { get; set; } = "GmsClient";

    /// <summary>HMAC-SHA256 signing key. MUST be provided (env/secret) in production; min 32 chars.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}

/// <summary>Custom JWT claim types used by GMS (kept unmapped: MapInboundClaims = false).</summary>
public static class GmsClaimTypes
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string FullName = "name";
    public const string Role = "role";
    public const string Permission = "perm";
    public const string Jti = "jti";
}
