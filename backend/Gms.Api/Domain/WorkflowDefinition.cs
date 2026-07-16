using Gms.Api.Common;

namespace Gms.Api.Domain;

/// <summary>
/// A reusable, versioned governance workflow (e.g. the default Change approval flow). The
/// definition is the stable container; its behaviour lives in versions. Exactly one Published
/// version may be Active at a time (<see cref="ActiveVersionId"/>). Definitions are resolved
/// for a trigger (object type + change class) when a governance object needs orchestration.
/// </summary>
public class WorkflowDefinition
{
    public Guid Id { get; set; }

    /// <summary>Stable machine code, e.g. CHANGE_NORMAL_DEFAULT (unique).</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Functional category, e.g. ChangeManagement.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Trigger object type (e.g. ChangeRequest) this workflow orchestrates.</summary>
    public string TriggerObjectType { get; set; } = string.Empty;

    /// <summary>Trigger event (e.g. ChangeSubmitted).</summary>
    public string TriggerEvent { get; set; } = string.Empty;

    /// <summary>For Change workflows: the ChangeClass this definition serves (Standard/Normal/Emergency). Null = not class-bound.</summary>
    public string? ChangeClass { get; set; }

    /// <summary>Draft | Active | Inactive | Archived (see <see cref="WorkflowDefinitionStatuses"/>).</summary>
    public string Status { get; set; } = WorkflowDefinitionStatuses.Draft;

    /// <summary>The currently active Published version (null until one is activated).</summary>
    public Guid? ActiveVersionId { get; set; }

    /// <summary>True for the seeded system defaults (protected from deletion).</summary>
    public bool IsSystem { get; set; }

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Relationships
    public ICollection<WorkflowVersion> Versions { get; set; } = new List<WorkflowVersion>();

    /// <summary>Guarded status transition — the aggregate owns its own lifecycle.</summary>
    public void TransitionTo(string target)
    {
        StatusTransition.Ensure(WorkflowDefinitionStatuses.Transitions, nameof(WorkflowDefinition), Status, target);
        Status = target;
    }
}
