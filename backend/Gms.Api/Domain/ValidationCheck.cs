namespace Gms.Api.Domain;

/// <summary>
/// One ordered validation check of a ValidationRun. Every check is treated as
/// mandatory: any failed check fails the whole run. Ordering (single active check,
/// no skipping ahead) is enforced by the ValidationService.
/// </summary>
public class ValidationCheck
{
    public Guid Id { get; set; }

    public Guid ValidationRunId { get; set; }
    public ValidationRun? ValidationRun { get; set; }

    public int CheckOrder { get; set; }
    public string Title { get; set; } = string.Empty;

    public string ExpectedResult { get; set; } = string.Empty;
    public string ActualResult { get; set; } = string.Empty;

    /// <summary>Waiting | Running | Passed | Failed | Skipped.</summary>
    public string Status { get; set; } = string.Empty;

    public DateTime? ExecutedAt { get; set; }

    public Guid? ExecutedByUserId { get; set; }
    public AppUser? ExecutedByUser { get; set; }

    public string Notes { get; set; } = string.Empty;
}
