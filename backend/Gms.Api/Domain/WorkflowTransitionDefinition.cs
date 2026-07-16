using Gms.Api.Common;

namespace Gms.Api.Domain;

/// <summary>
/// A directed edge between two steps of a workflow version. Outgoing transitions from a step
/// are evaluated in ascending <see cref="Priority"/>; the first whose condition matches wins
/// (Always is an unconditional fallback). Conditions reference ONLY allowlisted object fields
/// (no dynamic code) — see <see cref="WorkflowChangeFields"/>.
/// </summary>
public class WorkflowTransitionDefinition
{
    public Guid Id { get; set; }

    public Guid WorkflowVersionId { get; set; }
    public WorkflowVersion? WorkflowVersion { get; set; }

    /// <summary>Source step key.</summary>
    public string FromStepKey { get; set; } = string.Empty;

    /// <summary>Target step key.</summary>
    public string ToStepKey { get; set; } = string.Empty;

    /// <summary>Always | ObjectField | RiskLevel | Status | Boolean (see <see cref="WorkflowConditionTypes"/>).</summary>
    public string ConditionType { get; set; } = WorkflowConditionTypes.Always;

    /// <summary>Evaluation order among a step's outgoing transitions (lower first; first match wins).</summary>
    public int Priority { get; set; }

    /// <summary>Allowlisted object field the condition reads (null for Always).</summary>
    public string? ConditionField { get; set; }

    /// <summary>Comparison operator (see <see cref="WorkflowOperators"/>); null for Always.</summary>
    public string? Operator { get; set; }

    /// <summary>Value the field is compared against (string; numeric fields parsed at eval time).</summary>
    public string? ExpectedValue { get; set; }

    public string? Description { get; set; }
}
