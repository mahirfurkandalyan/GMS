using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Services.Auth;

/// <summary>
/// Owns the authentication lifecycle: login (with lockout), refresh-token rotation,
/// logout/logout-all, password change, and the "me" projection. Writes security audit
/// events for every action. Never returns password hashes or token hashes.
/// </summary>
public interface IAuthenticationService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ip, string? userAgent, CancellationToken ct = default);
    Task<AuthResponse> RefreshAsync(string rawRefreshToken, string? ip, string? userAgent, CancellationToken ct = default);
    Task LogoutAsync(Guid userId, string? rawRefreshToken, string? ip, string? userAgent, CancellationToken ct = default);
    Task LogoutAllAsync(Guid userId, string? ip, string? userAgent, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, string? ip, string? userAgent, CancellationToken ct = default);
    Task<AuthUserDto> GetMeAsync(Guid userId, CancellationToken ct = default);
}

public sealed class AuthenticationService : IAuthenticationService
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    private readonly GmsDbContext _db;
    private readonly IPasswordService _password;
    private readonly ITokenService _token;
    private readonly IPermissionService _permissions;
    private readonly Notifications.NotificationService _notifications;

    public AuthenticationService(GmsDbContext db, IPasswordService password, ITokenService token,
        IPermissionService permissions, Notifications.NotificationService notifications)
    {
        _db = db;
        _password = password;
        _token = token;
        _permissions = permissions;
        _notifications = notifications;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ip, string? userAgent, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var normalized = (request.Email ?? string.Empty).Trim().ToUpperInvariant();

        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, ct);

        // Generic failure for unknown/inactive user (no account enumeration).
        if (user is null || !user.IsActive)
        {
            AddAudit(null, request.Email, SecurityEventTypes.LoginFailed, SecurityEventResults.Failure, ip, userAgent, "Bilinmeyen veya pasif kullanıcı.");
            await _db.SaveChangesAsync(ct);
            throw new AuthFailedException("Geçersiz kimlik bilgileri.");
        }

        if (user.LockoutEnd is not null && user.LockoutEnd > now)
        {
            AddAudit(user.Id, user.Email, SecurityEventTypes.LoginFailed, SecurityEventResults.Failure, ip, userAgent, "Hesap kilitliyken giriş denemesi.");
            await _db.SaveChangesAsync(ct);
            throw new AuthFailedException("Hesap geçici olarak kilitlendi. Lütfen daha sonra tekrar deneyin.");
        }

        if (!_password.Verify(user, request.Password ?? string.Empty))
        {
            user.FailedLoginCount++;
            user.UpdatedAt = now;
            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.LockoutEnd = now.AddMinutes(LockoutMinutes);
                AddAudit(user.Id, user.Email, SecurityEventTypes.UserLockedOut, SecurityEventResults.Failure, ip, userAgent,
                    $"{MaxFailedAttempts} başarısız denemeden sonra {LockoutMinutes} dk kilitlendi.");
                await _notifications.NotifyUserAsync(user.Id, Common.NotificationTemplates.SecurityLockedOut,
                    Common.NotificationSeverities.Critical, null, createdBy: null, ct: ct);
            }
            else
            {
                AddAudit(user.Id, user.Email, SecurityEventTypes.LoginFailed, SecurityEventResults.Failure, ip, userAgent, "Hatalı parola.");
                await _notifications.NotifyUserAsync(user.Id, Common.NotificationTemplates.SecurityLoginFailed,
                    Common.NotificationSeverities.Warning, null, createdBy: null, ct: ct);
            }
            await _db.SaveChangesAsync(ct);
            throw new AuthFailedException("Geçersiz kimlik bilgileri.");
        }

        // Success — reset counters, refresh session.
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = now;
        user.UpdatedAt = now;

        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();
        var perms = await _permissions.GetPermissionsForRolesAsync(roles, ct);
        var (response, _) = BuildAuthResponse(user, roles, perms, ip, now);

        AddAudit(user.Id, user.Email, SecurityEventTypes.LoginSucceeded, SecurityEventResults.Success, ip, userAgent, "Giriş başarılı.");
        await _db.SaveChangesAsync(ct);
        return response;
    }

    public async Task<AuthResponse> RefreshAsync(string rawRefreshToken, string? ip, string? userAgent, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var hash = _token.HashRefreshToken(rawRefreshToken ?? string.Empty);

        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is null || !existing.IsActive(now))
        {
            AddAudit(existing?.AppUserId, string.Empty, SecurityEventTypes.TokenRefreshFailed, SecurityEventResults.Failure, ip, userAgent, "Geçersiz/iptal/süresi dolmuş yenileme jetonu.");
            await _db.SaveChangesAsync(ct);
            throw new AuthFailedException("Geçersiz veya süresi dolmuş yenileme jetonu.");
        }

        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == existing.AppUserId, ct);
        if (user is null || !user.IsActive)
            throw new AuthFailedException("Geçersiz veya süresi dolmuş yenileme jetonu.");

        // Rotate: revoke the presented token, issue a replacement.
        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();
        var perms = await _permissions.GetPermissionsForRolesAsync(roles, ct);
        var (response, newToken) = BuildAuthResponse(user, roles, perms, ip, now);

        existing.RevokedAt = now;
        existing.RevokedByIp = ip;
        existing.ReasonRevoked = "Rotated";
        existing.ReplacedByTokenId = newToken.Id;

        AddAudit(user.Id, user.Email, SecurityEventTypes.TokenRefreshed, SecurityEventResults.Success, ip, userAgent, "Yenileme jetonu döndürüldü.");
        await _db.SaveChangesAsync(ct);
        return response;
    }

    public async Task LogoutAsync(Guid userId, string? rawRefreshToken, string? ip, string? userAgent, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            var hash = _token.HashRefreshToken(rawRefreshToken);
            var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.AppUserId == userId, ct);
            if (token is not null && token.IsActive(now))
            {
                token.RevokedAt = now;
                token.RevokedByIp = ip;
                token.ReasonRevoked = "Logout";
            }
        }
        AddAudit(userId, string.Empty, SecurityEventTypes.Logout, SecurityEventResults.Success, ip, userAgent, "Oturum kapatıldı.");
        await _db.SaveChangesAsync(ct);
    }

    public async Task LogoutAllAsync(Guid userId, string? ip, string? userAgent, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await RevokeAllActiveTokensAsync(userId, ip, "LogoutAll", now, ct);
        AddAudit(userId, string.Empty, SecurityEventTypes.LogoutAll, SecurityEventResults.Success, ip, userAgent, "Tüm oturumlar kapatıldı.");
        await _db.SaveChangesAsync(ct);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, string? ip, string? userAgent, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        _password.ValidatePolicy(request.NewPassword ?? string.Empty);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new AuthFailedException("Kullanıcı bulunamadı.");

        if (!_password.Verify(user, request.CurrentPassword ?? string.Empty))
            throw new AuthValidationException("Mevcut parola hatalı.");

        user.PasswordHash = _password.Hash(user, request.NewPassword!);
        user.UpdatedAt = now;

        // Security: invalidate all existing sessions on password change.
        await RevokeAllActiveTokensAsync(userId, ip, "PasswordChanged", now, ct);

        AddAudit(userId, user.Email, SecurityEventTypes.PasswordChanged, SecurityEventResults.Success, ip, userAgent, "Parola değiştirildi; tüm oturumlar iptal edildi.");
        await _notifications.NotifyUserAsync(userId, Common.NotificationTemplates.PasswordChanged,
            Common.NotificationSeverities.Warning, null, createdBy: userId, ct: ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AuthUserDto> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new AuthFailedException("Kullanıcı bulunamadı.");

        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();
        var perms = await _permissions.GetPermissionsForRolesAsync(roles, ct);
        return MapUser(user, roles, perms);
    }

    /* ── Private helpers ─────────────────────────────────── */

    private (AuthResponse Response, RefreshToken Token) BuildAuthResponse(
        AppUser user, List<string> roles, IReadOnlyList<string> perms, string? ip, DateTime now)
    {
        var access = _token.CreateAccessToken(user, roles, perms);
        var refresh = _token.CreateRefreshToken(now);

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            AppUserId = user.Id,
            TokenHash = refresh.TokenHash,
            ExpiresAt = refresh.ExpiresAt,
            CreatedAt = now,
            CreatedByIp = ip
        };
        _db.RefreshTokens.Add(entity);

        var response = new AuthResponse
        {
            AccessToken = access.Token,
            RefreshToken = refresh.RawToken,
            AccessTokenExpiresAt = access.ExpiresAt,
            RefreshTokenExpiresAt = refresh.ExpiresAt,
            User = MapUser(user, roles, perms)
        };
        return (response, entity);
    }

    private async Task RevokeAllActiveTokensAsync(Guid userId, string? ip, string reason, DateTime now, CancellationToken ct)
    {
        var active = await _db.RefreshTokens
            .Where(t => t.AppUserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync(ct);
        foreach (var t in active)
        {
            t.RevokedAt = now;
            t.RevokedByIp = ip;
            t.ReasonRevoked = reason;
        }
    }

    private static AuthUserDto MapUser(AppUser user, List<string> roles, IReadOnlyList<string> perms) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Roles = roles,
        Permissions = perms.ToList()
    };

    private void AddAudit(Guid? userId, string? email, string eventType, string result, string? ip, string? userAgent, string description)
    {
        _db.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = email ?? string.Empty,
            EventType = eventType,
            Result = result,
            IpAddress = ip,
            UserAgent = userAgent,
            Description = description,
            CreatedAt = DateTime.UtcNow
        });
    }
}
