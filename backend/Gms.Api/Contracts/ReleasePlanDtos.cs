namespace Gms.Api.Contracts;

/// <summary>Summary row for the releases list.</summary>
public class ReleasePlanListDto
{
    public Guid Id { get; set; }
    public string ReleaseNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = string.Empty;
    public string ReleaseType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public int ChangeCount { get; set; }
    public int TotalEstimatedMinutes { get; set; }
    public DateTime? PlannedDeploymentStart { get; set; }
    public string ReleaseManagerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Full release plan detail.</summary>
public class ReleasePlanDetailDto
{
    public Guid Id { get; set; }
    public string ReleaseNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public Guid EnvironmentId { get; set; }
    public string EnvironmentName { get; set; } = string.Empty;
    public string ReleaseType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public int TotalEstimatedMinutes { get; set; }
    public DateTime? PlannedDeploymentStart { get; set; }
    public DateTime? PlannedDeploymentEnd { get; set; }
    public string RollbackWindow { get; set; } = string.Empty;
    public string BusinessOwner { get; set; } = string.Empty;
    public string TechnicalOwner { get; set; } = string.Empty;
    public Guid ReleaseManagerUserId { get; set; }
    public string ReleaseManagerName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Base64 concurrency token; echo it back on update to detect lost updates (409).</summary>
    public string RowVersion { get; set; } = string.Empty;

    public List<ReleasePlanItemDto> Items { get; set; } = new();
    public ReleaseDeploymentPlanDto? DeploymentPlan { get; set; }
    public List<ReleaseDocumentDto> Documents { get; set; } = new();
    public List<ReleaseAuditEventDto> AuditEvents { get; set; } = new();
}

public class ReleasePlanItemDto
{
    public Guid Id { get; set; }
    public Guid ChangeRequestId { get; set; }
    public string ChangeNo { get; set; } = string.Empty;
    public string ChangeTitle { get; set; } = string.Empty;
    public string ChangeStatus { get; set; } = string.Empty;
    public string ChangeRiskLevel { get; set; } = string.Empty;
    public int DeploymentOrder { get; set; }
    public int EstimatedMinutes { get; set; }
    public bool RollbackRequired { get; set; }
}

public class ReleaseDeploymentPlanDto
{
    public string DeploymentStrategy { get; set; } = string.Empty;
    public string CommunicationPlan { get; set; } = string.Empty;
    public string RollbackStrategy { get; set; } = string.Empty;
    public bool DowntimeExpected { get; set; }
    public int EstimatedDowntimeMinutes { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class ReleaseDocumentDto
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ReleaseAuditEventDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    /// <summary>Resolved actor display name (joined from Users) for the timeline.</summary>
    public string ActorUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/* ── Request DTOs ─────────────────────────────────────── */

public class CreateReleasePlanDto
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public string ReleaseType { get; set; } = string.Empty;
    public DateTime? PlannedDeploymentStart { get; set; }
    public DateTime? PlannedDeploymentEnd { get; set; }
    public string? RollbackWindow { get; set; }
    public string? BusinessOwner { get; set; }
    public string? TechnicalOwner { get; set; }
    public Guid ReleaseManagerUserId { get; set; }
    public string? Description { get; set; }
    // Actor comes from the authenticated context, not the request body.

    public List<Guid> ChangeIds { get; set; } = new();
    public CreateReleaseDeploymentPlanDto? DeploymentPlan { get; set; }
    public List<CreateReleaseDocumentDto> Documents { get; set; } = new();
}

public class CreateReleaseDeploymentPlanDto
{
    public string? DeploymentStrategy { get; set; }
    public string? CommunicationPlan { get; set; }
    public string? RollbackStrategy { get; set; }
    public bool DowntimeExpected { get; set; }
    public int EstimatedDowntimeMinutes { get; set; }
    public string? Notes { get; set; }
}

public class CreateReleaseDocumentDto
{
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string? Version { get; set; }
}

public class UpdateReleasePlanDto
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public string? ReleaseType { get; set; }
    public DateTime? PlannedDeploymentStart { get; set; }
    public DateTime? PlannedDeploymentEnd { get; set; }
    public string? RollbackWindow { get; set; }
    public string? BusinessOwner { get; set; }
    public string? TechnicalOwner { get; set; }
    public string? Description { get; set; }
    public CreateReleaseDeploymentPlanDto? DeploymentPlan { get; set; }
    // Actor comes from the authenticated context, not the request body.

    /// <summary>Optional base64 concurrency token; a mismatch yields 409 Conflict.</summary>
    public string? RowVersion { get; set; }
}
