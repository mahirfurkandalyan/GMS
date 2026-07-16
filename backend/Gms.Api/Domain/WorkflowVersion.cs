using Gms.Api.Common;

namespace Gms.Api.Domain;

/// <summary>
/// One immutable snapshot of a workflow's step/transition graph. Draft versions can be edited
/// and validated; once Published they are frozen (steps/transitions never change) so running
/// instances always reference a stable definition. Retired versions are kept for history.
/// </summary>
public class WorkflowVersion
{
    public Guid Id { get; set; }

    public Guid WorkflowDefinitionId { get; set; }
    public WorkflowDefinition? WorkflowDefinition { get; set; }

    /// <summary>Monotonic version number within the definition (1..).</summary>
    public int VersionNumber { get; set; }

    /// <summary>Draft | Published | Retired (see <see cref="WorkflowVersionStatuses"/>).</summary>
    public string Status { get; set; } = WorkflowVersionStatuses.Draft;

    /// <summary>Step key of the single Start step (resolved at publish; drives runtime entry).</summary>
    public string? StartStepKey { get; set; }

    public string? Notes { get; set; }

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? PublishedByUserId { get; set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Relationships
    public ICollection<WorkflowStepDefinition> Steps { get; set; } = new List<WorkflowStepDefinition>();
    public ICollection<WorkflowTransitionDefinition> Transitions { get; set; } = new List<WorkflowTransitionDefinition>();

    public bool IsPublished => Status == WorkflowVersionStatuses.Published;
}
