namespace Gms.Api.Domain;

/// <summary>Append-only audit event for a deployment run.</summary>
public class DeploymentEvent
{
    public Guid Id { get; set; }

    public Guid DeploymentRunId { get; set; }
    public DeploymentRun? DeploymentRun { get; set; }

    /// <summary>See <see cref="Gms.Api.Common.DeploymentEventTypes"/>.</summary>
    public string EventType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid ActorUserId { get; set; }
    public AppUser? ActorUser { get; set; }

    public DateTime CreatedAt { get; set; }
}
