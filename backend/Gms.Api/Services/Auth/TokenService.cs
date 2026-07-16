using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Gms.Api.Common;
using Gms.Api.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Gms.Api.Services.Auth;

public sealed record AccessTokenResult(string Token, DateTime ExpiresAt, string Jti);
public sealed record RefreshTokenResult(string RawToken, string TokenHash, DateTime ExpiresAt);

/// <summary>Issues JWT access tokens and opaque, hashed refresh tokens.</summary>
public interface ITokenService
{
    AccessTokenResult CreateAccessToken(AppUser user, IEnumerable<string> roles, IEnumerable<string> permissions);
    RefreshTokenResult CreateRefreshToken(DateTime utcNow);
    string HashRefreshToken(string rawToken);
}

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AccessTokenResult CreateAccessToken(AppUser user, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var jti = Guid.NewGuid().ToString("N");

        var claims = new List<Claim>
        {
            new(GmsClaimTypes.UserId, user.Id.ToString()),
            new(GmsClaimTypes.Email, user.Email),
            new(GmsClaimTypes.FullName, user.FullName),
            new(GmsClaimTypes.Jti, jti)
        };
        claims.AddRange(roles.Distinct().Select(r => new Claim(GmsClaimTypes.Role, r)));
        claims.AddRange(permissions.Distinct().Select(p => new Claim(GmsClaimTypes.Permission, p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessTokenResult(jwt, expiresAt, jti);
    }

    public RefreshTokenResult CreateRefreshToken(DateTime utcNow)
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Base64UrlEncode(bytes);
        var hash = HashRefreshToken(raw);
        var expiresAt = utcNow.AddDays(_options.RefreshTokenDays);
        return new RefreshTokenResult(raw, hash, expiresAt);
    }

    public string HashRefreshToken(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash); // 64 chars, deterministic, safe to index
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
