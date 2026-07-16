using Gms.Api.Common;

namespace Gms.Api.Domain;

/// <summary>
/// One integration operation (aggregate root of the runtime side). Acts as the integration
/// OUTBOX: outgoing deliveries are created as <c>Pending</c> inside the business transaction and
/// processed later by the dispatcher, so an external HTTP side effect never happens before the
/// database commit. Summaries are sanitized — no raw secrets or full sensitive payloads.
/// </summary>
public class IntegrationExecution
{
    public Guid Id { get; set; }

    /// <summary>Human-readable number, e.g. INX-2026-000001.</summary>
    public string ExecutionNo { get; set; } = string.Empty;

    public Guid IntegrationDefinitionId { get; set; }
    public IntegrationDefinition? IntegrationDefinition { get; set; }

    /// <summary>Incoming | Outgoing.</summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>Operation identifier (e.g. the GMS event type or a webhook mapping).</summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>Internal object type/id this operation concerns (optional).</summary>
    public string? ObjectType { get; set; }
    public Guid? ObjectId { get; set; }

    /// <summary>Correlation id for tracing a delivery across attempts and audit.</summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>Pending | Running | Succeeded | Failed | Cancelled | DeadLetter.</summary>
    public string Status { get; set; } = IntegrationExecutionStatuses.Pending;

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Sanitized request summary (method, path, size) — never secrets/full payload.</summary>
    public string? RequestSummary { get; set; }

    /// <summary>Sanitized, length-limited response summary — never secrets.</summary>
    public string? ResponseSummary { get; set; }

    public int? HttpStatusCode { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    /// <summary>Earliest time a retryable (Failed) execution may be re-dispatched (backoff window).</summary>
    public DateTime? NextAttemptAt { get; set; }

    /// <summary>Lease expiry — while in the future the row is claimed and other workers skip it.</summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>Node instance that currently holds the lease (null when unclaimed).</summary>
    public string? LockedBy { get; set; }

    /// <summary>Actor who triggered this (null for system/event-driven deliveries).</summary>
    public Guid? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Relationships
    public ICollection<IntegrationExecutionAttempt> Attempts { get; set; } = new List<IntegrationExecutionAttempt>();
    public ICollection<IntegrationEvent> Events { get; set; } = new List<IntegrationEvent>();

    /// <summary>Guarded status transition — the aggregate owns its own lifecycle.</summary>
    public void TransitionTo(string target)
    {
        StatusTransition.Ensure(IntegrationExecutionStatuses.Transitions, nameof(IntegrationExecution), Status, target);
        Status = target;
    }
}
