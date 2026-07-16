namespace Gms.Api.Contracts;

/// <summary>Summary row for the change-request list.</summary>
public class ChangeRequestListDto
{
    public Guid Id { get; set; }
    public string ChangeNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = string.Empty;
    public string ChangeClass { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public DateTime? PlannedImplementationDate { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Full change-request detail, including latest revision and readiness.</summary>
public class ChangeRequestDetailDto
{
    public Guid Id { get; set; }
    public string ChangeNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BusinessReason { get; set; } = string.Empty;

    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid EnvironmentId { get; set; }
    public string EnvironmentName { get; set; } = string.Empty;

    public string ChangeClass { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public int RiskScore { get; set; }

    public DateTime? PlannedImplementationDate { get; set; }
    public DateTime? PlannedRollbackDate { get; set; }
    public string? SourceSystem { get; set; }
    public string? SourceReference { get; set; }

    public Guid CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Base64 concurrency token; echo it back on update to detect lost updates (409).</summary>
    public string RowVersion { get; set; } = string.Empty;

    public ChangeRevisionDto? LatestRevision { get; set; }
    public List<ChangeAffectedAssetDto> Assets { get; set; } = new();
    public List<ChangeDocumentDto> Documents { get; set; } = new();
    public List<ChangeAuditEventDto> AuditEvents { get; set; } = new();
    public ChangeReadinessDto Readiness { get; set; } = new();
}

public class ChangeRevisionDto
{
    public Guid Id { get; set; }
    public int RevisionNo { get; set; }
    public string TechnicalSummary { get; set; } = string.Empty;
    public string ImplementationNotes { get; set; } = string.Empty;
    public string DeploymentInstructions { get; set; } = string.Empty;
    public string SqlScript { get; set; } = string.Empty;
    public string RollbackScript { get; set; } = string.Empty;
    public string RollbackStrategy { get; set; } = string.Empty;
    public string RollbackOwner { get; set; } = string.Empty;
    public int EstimatedDurationMinutes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ChangeAffectedAssetDto
{
    public Guid Id { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string Criticality { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ChangeDocumentDto
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ChangeAuditEventDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    /// <summary>Resolved actor display name (joined from Users) for the timeline.</summary>
    public string ActorUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ChangeReadinessDto
{
    public int ReadinessScore { get; set; }
    public List<ChangeReadinessFindingDto> Findings { get; set; } = new();
}

public class ChangeReadinessFindingDto
{
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}

/* ── Request DTOs ─────────────────────────────────────── */

public class CreateChangeRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BusinessReason { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public string ChangeClass { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime? PlannedImplementationDate { get; set; }
    public DateTime? PlannedRollbackDate { get; set; }
    public string? SourceSystem { get; set; }
    public string? SourceReference { get; set; }
    // Actor (creator) comes from the authenticated context, not the request body.

    public CreateChangeRevisionDto Revision { get; set; } = new();
    public List<CreateChangeAffectedAssetDto> Assets { get; set; } = new();
    public List<CreateChangeDocumentDto> Documents { get; set; } = new();
}

public class UpdateChangeRequestDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? BusinessReason { get; set; }
    public Guid? EnvironmentId { get; set; }
    public string? ChangeClass { get; set; }
    public string? ChangeType { get; set; }
    public string? Priority { get; set; }
    public DateTime? PlannedImplementationDate { get; set; }
    public DateTime? PlannedRollbackDate { get; set; }
    public string? SourceSystem { get; set; }
    public string? SourceReference { get; set; }
    // Actor comes from the authenticated context, not the request body.

    /// <summary>Optional base64 concurrency token from the loaded record. When supplied,
    /// a mismatch yields 409 Conflict (optimistic concurrency). When null, no check is enforced.</summary>
    public string? RowVersion { get; set; }
}

public class CreateChangeRevisionDto
{
    public string TechnicalSummary { get; set; } = string.Empty;
    public string ImplementationNotes { get; set; } = string.Empty;
    public string DeploymentInstructions { get; set; } = string.Empty;
    public string SqlScript { get; set; } = string.Empty;
    public string RollbackScript { get; set; } = string.Empty;
    public string RollbackStrategy { get; set; } = string.Empty;
    public string RollbackOwner { get; set; } = string.Empty;
    public int EstimatedDurationMinutes { get; set; }
    // Actor (creator) comes from the authenticated context, not the request body.
}

public class CreateChangeAffectedAssetDto
{
    public string AssetType { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string Criticality { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class CreateChangeDocumentDto
{
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
