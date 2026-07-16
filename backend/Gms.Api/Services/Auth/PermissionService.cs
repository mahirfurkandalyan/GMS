using Gms.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Services.Auth;

/// <summary>Resolves the effective permission set for a set of roles from the database
/// (the authoritative role → permission grants). Used at login/refresh to build claims.</summary>
public interface IPermissionService
{
    Task<IReadOnlyList<string>> GetPermissionsForRolesAsync(IEnumerable<string> roleNames, CancellationToken ct = default);
}

public sealed class PermissionService : IPermissionService
{
    private readonly GmsDbContext _db;

    public PermissionService(GmsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<string>> GetPermissionsForRolesAsync(IEnumerable<string> roleNames, CancellationToken ct = default)
    {
        var names = roleNames.Distinct().ToList();
        if (names.Count == 0) return Array.Empty<string>();

        return await _db.RolePermissions
            .Where(rp => names.Contains(rp.Role!.Name))
            .Select(rp => rp.Permission!.Code)
            .Distinct()
            .ToListAsync(ct);
    }
}
