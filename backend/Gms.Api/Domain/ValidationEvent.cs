namespace Gms.Api.Domain;

/// <summary>Append-only audit event for a validation run.</summary>
public class ValidationEvent
{
    public Guid Id { get; set; }

    public Guid ValidationRunId { get; set; }
    public ValidationRun? ValidationRun { get; set; }

    /// <summary>See <see cref="Gms.Api.Common.ValidationEventTypes"/>.</summary>
    public string EventType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid ActorUserId { get; set; }
    public AppUser? ActorUser { get; set; }

    public DateTime CreatedAt { get; set; }
}
