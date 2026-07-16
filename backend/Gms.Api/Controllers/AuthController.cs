using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Controllers;

/// <summary>
/// Real authentication: JWT access tokens + rotating refresh tokens, backed by
/// server-side identity. Thin controller — all lifecycle logic lives in
/// <see cref="IAuthenticationService"/>.
/// </summary>
[ApiController]
[Route("api/auth")]
[Tags("Auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _auth;
    private readonly ICurrentUser _currentUser;

    public AuthController(IAuthenticationService auth, ICurrentUser currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    private string? Ip => HttpContext.Connection.RemoteIpAddress?.ToString();
    private string? Agent => Request.Headers.UserAgent.ToString();

    /// <summary>Email + parola ile giriş; access + refresh jeton çifti döndürür.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        => Ok(await _auth.LoginAsync(request, Ip, Agent));

    /// <summary>Refresh jetonunu döndürür (rotation): eski jeton iptal, yeni çift verilir.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request)
        => Ok(await _auth.RefreshAsync(request.RefreshToken, Ip, Agent));

    /// <summary>Oturumu kapatır; verilirse refresh jetonunu iptal eder.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request)
    {
        await _auth.LogoutAsync(_currentUser.RequireUserId(), request?.RefreshToken, Ip, Agent);
        return NoContent();
    }

    /// <summary>Kullanıcının tüm aktif refresh jetonlarını iptal eder.</summary>
    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll()
    {
        await _auth.LogoutAllAsync(_currentUser.RequireUserId(), Ip, Agent);
        return NoContent();
    }

    /// <summary>Kimliği doğrulanmış kullanıcı, rolleri ve izinleri.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AuthUserDto>> Me()
        => Ok(await _auth.GetMeAsync(_currentUser.RequireUserId()));

    /// <summary>Parola değiştirir; başarıda tüm aktif oturumları iptal eder.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        await _auth.ChangePasswordAsync(_currentUser.RequireUserId(), request, Ip, Agent);
        return NoContent();
    }

    /// <summary>
    /// DEPRECATED — yalnızca Development. Frontend'in giriş ekranını doldurması için
    /// seed edilmiş kullanıcı e-postalarını döndürür. Production'da 404.
    /// </summary>
    [HttpGet("mock-users")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<MockUserDto>>> GetMockUsers(
        [FromServices] IWebHostEnvironment env, [FromServices] GmsDbContext db)
    {
        if (!env.IsDevelopment())
            return NotFound();

        var users = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.CreatedAt).ThenBy(u => u.FullName)
            .Select(u => new MockUserDto
            {
                Id = u.Id, FullName = u.FullName, Email = u.Email,
                Roles = u.UserRoles.Select(ur => ur.Role!.Name).ToList()
            })
            .ToListAsync();

        return Ok(users);
    }
}
