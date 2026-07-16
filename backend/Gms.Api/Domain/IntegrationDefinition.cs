using Gms.Api.Common;

namespace Gms.Api.Domain;

/// <summary>
/// A configured external integration (aggregate root of the Integration Hub). Carries provider,
/// category, base URL and authentication type; owns its credentials, endpoints, subscriptions and
/// audit events. Secret values are never stored here — only in <see cref="IntegrationCredential"/>
/// (protected). This is the single infrastructure every module uses to talk to external systems.
/// </summary>
public class IntegrationDefinition
{
    public Guid Id { get; set; }

    /// <summary>Human-readable number, e.g. INT-2026-000001.</summary>
    public string IntegrationNo { get; set; } = string.Empty;

    /// <summary>Stable machine code (unique); also used as the incoming-webhook path segment.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Provider adapter selector (see <see cref="IntegrationProviders"/>).</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Functional category (see <see cref="IntegrationCategories"/>).</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Draft | Active | Inactive | Failed | Archived.</summary>
    public string Status { get; set; } = IntegrationStatuses.Draft;

    /// <summary>Base URL for outgoing requests (nullable for pure-incoming integrations).</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Authentication type (see <see cref="IntegrationAuthTypes"/>).</summary>
    public string AuthenticationType { get; set; } = IntegrationAuthTypes.None;

    /// <summary>True for seeded system defaults (protected from deletion).</summary>
    public bool IsSystem { get; set; }

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastSuccessfulConnectionAt { get; set; }
    public DateTime? LastFailedConnectionAt { get; set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Relationships
    public ICollection<IntegrationCredential> Credentials { get; set; } = new List<IntegrationCredential>();
    public ICollection<IntegrationEndpoint> Endpoints { get; set; } = new List<IntegrationEndpoint>();
    public ICollection<IntegrationSubscription> Subscriptions { get; set; } = new List<IntegrationSubscription>();
    public ICollection<IntegrationEvent> Events { get; set; } = new List<IntegrationEvent>();

    /// <summary>Guarded status transition — the aggregate owns its own lifecycle.</summary>
    public void TransitionTo(string target)
    {
        StatusTransition.Ensure(IntegrationStatuses.Transitions, nameof(IntegrationDefinition), Status, target);
        Status = target;
    }
}
