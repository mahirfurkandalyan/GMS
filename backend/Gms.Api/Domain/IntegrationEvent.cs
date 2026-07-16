namespace Gms.Api.Domain;

/// <summary>
/// Append-only audit event for the Integration Hub (creation, activation, credential rotation,
/// connection tests, incoming/outgoing deliveries, retries, dead-letter, link changes). Feeds the
/// unified audit read model under the INTEGRATION module. Never mutated after creation.
/// </summary>
public class IntegrationEvent
{
    public Guid Id { get; set; }

    /// <summary>Optional execution this event relates to (null for definition-level events).</summary>
    public Guid? IntegrationExecutionId { get; set; }
    public IntegrationExecution? IntegrationExecution { get; set; }

    public Guid IntegrationDefinitionId { get; set; }
    public IntegrationDefinition? IntegrationDefinition { get; set; }

    /// <summary>Actor who caused the event; null for system/external-driven events.</summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>See <see cref="Gms.Api.Common.IntegrationEventTypes"/>.</summary>
    public string EventType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
