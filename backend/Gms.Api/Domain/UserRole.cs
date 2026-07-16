namespace Gms.Api.Domain;

/// <summary>
/// User-role assignment (many-to-many join). A user may hold multiple roles.
/// A unique index on (AppUserId, RoleId) prevents duplicate assignments.
/// </summary>
public class UserRole
{
    public Guid Id { get; set; }

    public Guid AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public Guid RoleId { get; set; }
    public Role? Role { get; set; }

    public DateTime AssignedAt { get; set; }

    /// <summary>User who assigned the role (nullable for seed data).</summary>
    public Guid? AssignedByUserId { get; set; }
}
