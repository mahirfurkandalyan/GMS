namespace Gms.Api.Domain;

/// <summary>
/// Read-only, keyless projection over the SQL view <c>vw_UnifiedAuditRecords</c>, which
/// UNION ALLs the domain-specific append-only audit tables (Change/Approval/Release/
/// Execution/Validation/Document/Security/Notification). This is a READ MODEL only — the
/// domain audit tables remain the authoritative source of truth and are never modified
/// through this type. Not every source populates every field; nulls are expected.
/// </summary>
public class UnifiedAuditRecord
{
    public Guid RecordId { get; set; }
    public string SourceModule { get; set; } = string.Empty;
    public string SourceTable { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public string ObjectType { get; set; } = string.Empty;
    public Guid? ObjectId { get; set; }
    public string? ObjectNumber { get; set; }
    public Guid? RelatedProjectId { get; set; }
    public Guid? RelatedEnvironmentId { get; set; }
    public string? Result { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
