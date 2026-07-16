namespace Gms.Api.Domain;

/// <summary>
/// A fine-grained permission in "module.action" form (e.g. "change.create").
/// Code is unique. Permissions are granted to roles via <see cref="RolePermission"/>.
/// </summary>
public class Permission
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Owning module: CHANGE | APPROVAL | RELEASE | EXECUTION | VALIDATION | AUDIT | ADMINISTRATION.</summary>
    public string Module { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Relationships
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
