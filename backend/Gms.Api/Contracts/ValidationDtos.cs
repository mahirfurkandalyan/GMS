namespace Gms.Api.Contracts;

/// <summary>Summary row for the validations list.</summary>
public class ValidationRunListDto
{
    public Guid Id { get; set; }
    public string ValidationNo { get; set; } = string.Empty;
    public Guid DeploymentRunId { get; set; }
    public string ExecutionNo { get; set; } = string.Empty;
    public string ReleaseNo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ValidationType { get; set; } = string.Empty;
    public string OverallResult { get; set; } = string.Empty;
    public int CheckCount { get; set; }
    public int PassedCheckCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string ValidatedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Full validation run detail: checks, evidence and audit events.</summary>
public class ValidationRunDetailDto
{
    public Guid Id { get; set; }
    public string ValidationNo { get; set; } = string.Empty;
    public Guid DeploymentRunId { get; set; }
    public string ExecutionNo { get; set; } = string.Empty;
    public string ReleaseNo { get; set; } = string.Empty;
    public string ReleaseStatus { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ValidationType { get; set; } = string.Empty;
    public string OverallResult { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid ValidatedByUserId { get; set; }
    public string ValidatedByUserName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>Base64 concurrency token; echo it back on actions to detect conflicts (409).</summary>
    public string RowVersion { get; set; } = string.Empty;

    public List<ValidationCheckDto> Checks { get; set; } = new();
    public List<ValidationEvidenceDto> Evidence { get; set; } = new();
    public List<ValidationEventDto> Events { get; set; } = new();
}

/// <summary>One ordered validation check.</summary>
public class ValidationCheckDto
{
    public Guid Id { get; set; }
    public int CheckOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ExpectedResult { get; set; } = string.Empty;
    public string ActualResult { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ExecutedAt { get; set; }
    public Guid? ExecutedByUserId { get; set; }
    public string? ExecutedByUserName { get; set; }
    public string Notes { get; set; } = string.Empty;
}

/// <summary>Validation evidence metadata.</summary>
public class ValidationEvidenceDto
{
    public Guid Id { get; set; }
    public string EvidenceType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Append-only validation audit event.</summary>
public class ValidationEventDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Optional check definition supplied at creation.</summary>
public class CreateValidationCheckDto
{
    public string Title { get; set; } = string.Empty;
    public string? ExpectedResult { get; set; }
}

/// <summary>Optional evidence supplied at creation (metadata only — no file upload).</summary>
public class CreateValidationEvidenceDto
{
    public string EvidenceType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Request body to create a validation run for a completed deployment. If Checks is
/// empty, one check per release change is generated automatically.
/// </summary>
public class CreateValidationRunDto
{
    public Guid DeploymentRunId { get; set; }
    // Actor comes from the authenticated context, not the request body.
    public string? ValidationType { get; set; }
    public string? Summary { get; set; }
    public List<CreateValidationCheckDto>? Checks { get; set; }
    public List<CreateValidationEvidenceDto>? Evidence { get; set; }
}

/// <summary>
/// Request body for run/check actions (start, start-next-check, pass-check, fail-check).
/// ActualResult records what actually happened; Notes doubles as the failure reason.
/// </summary>
public class ValidationActionDto
{
    // Actor comes from the authenticated context, not the request body.
    public string? ActualResult { get; set; }
    public string? Notes { get; set; }

    /// <summary>Optional base64 concurrency token; a mismatch yields 409 Conflict.</summary>
    public string? RowVersion { get; set; }
}
