namespace Gms.Api.Domain;

/// <summary>
/// Role-permission grant (many-to-many join). A unique index on (RoleId, PermissionId)
/// prevents duplicate grants.
/// </summary>
public class RolePermission
{
    public Guid Id { get; set; }

    public Guid RoleId { get; set; }
    public Role? Role { get; set; }

    public Guid PermissionId { get; set; }
    public Permission? Permission { get; set; }

    public DateTime AssignedAt { get; set; }
}
