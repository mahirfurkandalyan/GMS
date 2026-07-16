namespace Gms.Api.Contracts;

/// <summary>Summary row for the approvals list.</summary>
public class ApprovalRequestListDto
{
    public Guid Id { get; set; }
    public string ApprovalNo { get; set; } = string.Empty;
    public string RelatedObjectType { get; set; } = string.Empty;
    public Guid RelatedObjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string RequestedByUserName { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int StepCount { get; set; }
    public int CurrentStepNo { get; set; }
    public string? CurrentStepName { get; set; }
}

/// <summary>Full approval detail.</summary>
public class ApprovalRequestDetailDto
{
    public Guid Id { get; set; }
    public string ApprovalNo { get; set; } = string.Empty;
    public string RelatedObjectType { get; set; } = string.Empty;
    public Guid RelatedObjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public Guid RequestedByUserId { get; set; }
    public string RequestedByUserName { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Base64 concurrency token (read-only view; approval mutations are action-based).</summary>
    public string RowVersion { get; set; } = string.Empty;

    public List<ApprovalStepDto> Steps { get; set; } = new();
    public List<ApprovalDecisionDto> Decisions { get; set; } = new();
    public List<ApprovalAuditEventDto> AuditEvents { get; set; } = new();
    public RelatedObjectSummaryDto? RelatedObject { get; set; }
}

public class ApprovalStepDto
{
    public Guid Id { get; set; }
    public int StepNo { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string ApproverRole { get; set; } = string.Empty;
    public Guid? ApproverUserId { get; set; }
    public string? ApproverUserName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class ApprovalDecisionDto
{
    public Guid Id { get; set; }
    public Guid ApprovalStepId { get; set; }
    public int StepNo { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public string SignatureMeaning { get; set; } = string.Empty;
    public Guid SignedByUserId { get; set; }
    public string SignedByUserName { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; }
}

public class ApprovalAuditEventDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Request body for approve / reject / request-revision actions.</summary>
public class ApprovalActionDto
{
    // Actor comes from the authenticated context, not the request body.
    public string? Comment { get; set; }
    public string? SignatureMeaning { get; set; }
}

/// <summary>Lightweight summary of the object an approval targets.</summary>
public class RelatedObjectSummaryDto
{
    public string Type { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = string.Empty;
}
