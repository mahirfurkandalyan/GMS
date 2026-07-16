namespace Gms.Api.Domain;

/// <summary>
/// A normalized link between an internal GMS object (e.g. ChangeRequest) and an external system
/// object (e.g. a Jira issue or Azure DevOps work item), resolved through an integration. This is
/// the canonical relationship; the Change's SourceSystem/SourceReference fields remain for
/// display/backward compatibility. Uniqueness prevents duplicate links for the same relationship.
/// </summary>
public class ExternalObjectLink
{
    public Guid Id { get; set; }

    public Guid IntegrationDefinitionId { get; set; }
    public IntegrationDefinition? IntegrationDefinition { get; set; }

    /// <summary>Internal object type (e.g. ChangeRequest, ReleasePlan, DeploymentRun).</summary>
    public string InternalObjectType { get; set; } = string.Empty;
    public Guid InternalObjectId { get; set; }

    /// <summary>External object type (e.g. JiraIssue, AzureDevOpsWorkItem).</summary>
    public string ExternalObjectType { get; set; } = string.Empty;

    /// <summary>Provider-normalized external id (e.g. work item id, issue id).</summary>
    public string ExternalObjectId { get; set; } = string.Empty;

    /// <summary>Human external key (e.g. Jira issue key EBR-421).</summary>
    public string? ExternalObjectKey { get; set; }

    /// <summary>Deep link to the external object (nullable).</summary>
    public string? ExternalUrl { get; set; }

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }
}
