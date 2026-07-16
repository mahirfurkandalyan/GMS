namespace Gms.Api.Domain;

/// <summary>
/// Binds a GMS domain event (e.g. WorkflowCompleted) to an outgoing endpoint of an integration.
/// When the event is published, a Pending <see cref="IntegrationExecution"/> is created for each
/// active matching subscription (outbox-ready delivery). Belongs to one integration.
/// </summary>
public class IntegrationSubscription
{
    public Guid Id { get; set; }

    public Guid IntegrationDefinitionId { get; set; }
    public IntegrationDefinition? IntegrationDefinition { get; set; }

    /// <summary>GMS event type (see <see cref="Gms.Api.Common.IntegrationSubscriptionEvents"/>).</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Internal object type the event concerns (e.g. WorkflowInstance, ReleasePlan).</summary>
    public string? ObjectType { get; set; }

    /// <summary>Outgoing endpoint the event is delivered to.</summary>
    public Guid TargetEndpointId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
