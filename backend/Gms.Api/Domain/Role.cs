namespace Gms.Api.Domain;

/// <summary>
/// Application role (Requester, Architect, QA, ReleaseManager, Executor, Validator,
/// Auditor, Admin). Role names are unique. A role grants a set of permissions.
/// </summary>
public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>System roles are seeded and must not be deleted from the UI.</summary>
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    // Relationships
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
