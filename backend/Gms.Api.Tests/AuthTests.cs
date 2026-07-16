using System.Net;
using System.Net.Http.Json;
using Gms.Api.Contracts;
using Xunit;

namespace Gms.Api.Tests;

[Collection("gms")]
public sealed class AuthTests
{
    private readonly GmsWebApplicationFactory _factory;
    public AuthTests(GmsWebApplicationFactory factory) => _factory = factory;

    [Fact] // 1
    public async Task Login_WithValidCredentials_ReturnsTokensAndUser()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = Seed.Architect, Password = GmsWebApplicationFactory.DevPassword });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrEmpty(body!.AccessToken));
        Assert.False(string.IsNullOrEmpty(body.RefreshToken));
        Assert.Contains("Architect", body.User.Roles);
        Assert.Contains("approval.approve.architect", body.User.Permissions);
    }

    [Fact] // 2
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = Seed.Admin, Password = "definitely-wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact] // 3
    public async Task Login_AfterFiveFailures_LocksAccount()
    {
        var client = _factory.CreateClient();
        for (var i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest { Email = Seed.Validator, Password = $"wrong-{i}" });
        }
        // Even with the correct password the account is now locked out.
        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = Seed.Validator, Password = GmsWebApplicationFactory.DevPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact] // 4 + 5
    public async Task Refresh_RotatesToken_AndOldTokenIsRejected()
    {
        var login = await _factory.LoginAsync(Seed.Admin);
        var client = _factory.CreateClient();

        var r1 = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest { RefreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        var rotated = await r1.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotEqual(login.RefreshToken, rotated!.RefreshToken);

        // Old (rotated) refresh token must be rejected.
        var reuse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest { RefreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        // New refresh token still works.
        var r2 = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest { RefreshToken = rotated.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
    }

    [Fact] // 6
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/change-requests?pageSize=1");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact] // 12
    public async Task ChangePassword_RevokesActiveRefreshTokens()
    {
        // Use auditor (not used for login elsewhere) to avoid cross-test interference.
        var login = await _factory.LoginAsync(Seed.Auditor);
        var authed = _factory.CreateClient();
        authed.DefaultRequestHeaders.Authorization = new("Bearer", login.AccessToken);

        const string newPassword = "Gms.New.2026!";
        var change = await authed.PostAsJsonAsync("/api/auth/change-password",
            new ChangePasswordRequest { CurrentPassword = GmsWebApplicationFactory.DevPassword, NewPassword = newPassword });
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        // The refresh token issued before the password change must now be revoked.
        var anon = _factory.CreateClient();
        var refresh = await anon.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest { RefreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        // Restore the shared seed password so other tests in the collection are unaffected.
        var reLogin = await _factory.LoginAsync(Seed.Auditor, newPassword);
        var reClient = _factory.CreateClient();
        reClient.DefaultRequestHeaders.Authorization = new("Bearer", reLogin.AccessToken);
        await reClient.PostAsJsonAsync("/api/auth/change-password",
            new ChangePasswordRequest { CurrentPassword = newPassword, NewPassword = GmsWebApplicationFactory.DevPassword });
    }
}
