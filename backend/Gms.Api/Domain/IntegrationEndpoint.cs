namespace Gms.Api.Domain;

/// <summary>
/// A named request surface of an integration. Incoming endpoints describe a webhook the external
/// system calls; Outgoing endpoints describe a relative path GMS calls (combined with the
/// definition's BaseUrl). Belongs to one <see cref="IntegrationDefinition"/>.
/// </summary>
public class IntegrationEndpoint
{
    public Guid Id { get; set; }

    public Guid IntegrationDefinitionId { get; set; }
    public IntegrationDefinition? IntegrationDefinition { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Incoming | Outgoing (see <see cref="Gms.Api.Common.IntegrationDirections"/>).</summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>Relative path (outgoing) or route hint (incoming).</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>HTTP method for outgoing requests (GET/POST/...).</summary>
    public string HttpMethod { get; set; } = "POST";

    public int TimeoutSeconds { get; set; } = 30;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
