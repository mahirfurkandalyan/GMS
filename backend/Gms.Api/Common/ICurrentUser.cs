using System.Security.Claims;

namespace Gms.Api.Common;

/// <summary>
/// The authenticated acting user, resolved from the server-side security context
/// (JWT claims) — never from client-supplied ids. Controllers and services read the
/// actor from here.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    string? FullName { get; }
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }
    bool IsAuthenticated { get; }
    bool HasRole(string role);
    bool HasPermission(string permission);

    /// <summary>UserId or throws — for endpoints that require an authenticated actor.</summary>
    Guid RequireUserId();
}

/// <summary>
/// Real implementation backed by <see cref="HttpContext.User"/> claims populated by the
/// JWT bearer handler. Replaces the previous header-based (X-Actor-User-Id) mechanism,
/// which is no longer accepted as an identity source.
/// </summary>
public sealed class JwtCurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal? _principal;

    public JwtCurrentUser(IHttpContextAccessor accessor)
    {
        _principal = accessor.HttpContext?.User;
    }

    public bool IsAuthenticated => _principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var raw = FindFirst(GmsClaimTypes.UserId, ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Email => FindFirst(GmsClaimTypes.Email, ClaimTypes.Email);
    public string? FullName => FindFirst(GmsClaimTypes.FullName, ClaimTypes.Name);

    public IReadOnlyCollection<string> Roles => FindAll(GmsClaimTypes.Role, ClaimTypes.Role);
    public IReadOnlyCollection<string> Permissions => FindAll(GmsClaimTypes.Permission);

    public bool HasRole(string role) => Roles.Contains(role);
    public bool HasPermission(string permission) => Permissions.Contains(permission);

    public Guid RequireUserId() =>
        UserId ?? throw new UnauthorizedAccessException("Kimliği doğrulanmış kullanıcı bulunamadı.");

    private string? FindFirst(params string[] types)
    {
        if (_principal is null) return null;
        foreach (var t in types)
        {
            var v = _principal.FindFirst(t)?.Value;
            if (!string.IsNullOrEmpty(v)) return v;
        }
        return null;
    }

    private IReadOnlyCollection<string> FindAll(params string[] types)
    {
        if (_principal is null) return Array.Empty<string>();
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in types)
            foreach (var c in _principal.FindAll(t))
                set.Add(c.Value);
        return set;
    }
}
