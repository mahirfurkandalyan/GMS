using Microsoft.AspNetCore.Authorization;

namespace Gms.Api.Common.Authorization;

/// <summary>Requires a specific permission code (from a "perm" claim in the access token).</summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) => Permission = permission;
}

/// <summary>
/// Grants access when the authenticated principal carries the required "perm" claim.
/// Permissions are baked into the JWT at login, so this is a stateless check (no DB hit).
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var hasPermission = context.User.Claims
            .Any(c => c.Type == GmsClaimTypes.Permission && c.Value == requirement.Permission);

        if (hasPermission) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Registers one authorization policy per permission code (policy name == permission
/// code), so controllers use [Authorize(Policy = Permissions.ChangeCreate)].
/// </summary>
public static class AuthorizationSetup
{
    public static void AddPermissionPolicies(this AuthorizationOptions options)
    {
        foreach (var code in Permissions.AllCodes)
        {
            options.AddPolicy(code, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new PermissionRequirement(code));
            });
        }
    }
}
