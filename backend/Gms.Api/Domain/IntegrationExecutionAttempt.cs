namespace Gms.Api.Domain;

/// <summary>
/// One delivery attempt of an <see cref="IntegrationExecution"/>. Records the attempt number,
/// timing, resulting HTTP status and any error, so the retry history is fully auditable.
/// </summary>
public class IntegrationExecutionAttempt
{
    public Guid Id { get; set; }

    public Guid IntegrationExecutionId { get; set; }
    public IntegrationExecution? IntegrationExecution { get; set; }

    /// <summary>1-based attempt number within the execution.</summary>
    public int AttemptNo { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Succeeded | Failed (per-attempt outcome).</summary>
    public string Status { get; set; } = string.Empty;

    public int? HttpStatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int DurationMilliseconds { get; set; }

    public DateTime CreatedAt { get; set; }
}
