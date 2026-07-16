using Gms.Api.Common;

namespace Gms.Api.Domain;

/// <summary>
/// A single node in a workflow version's graph. StepType decides runtime behaviour: Start/End
/// are structural, Condition routes by evaluating outgoing transitions, Notification fires a
/// template, and ManualTask/Approval pause the instance until a human acts. Assignment
/// (role/user) applies to manual steps. Belongs to an immutable Published version.
/// </summary>
public class WorkflowStepDefinition
{
    public Guid Id { get; set; }

    public Guid WorkflowVersionId { get; set; }
    public WorkflowVersion? WorkflowVersion { get; set; }

    /// <summary>Stable key within the version (e.g. START, ARCH, END). Referenced by transitions.</summary>
    public string StepKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Start | ManualTask | Approval | Condition | Notification | End (see <see cref="WorkflowStepTypes"/>).</summary>
    public string StepType { get; set; } = string.Empty;

    /// <summary>Ordering hint for display / deterministic processing (1..).</summary>
    public int StepOrder { get; set; }

    /// <summary>For manual steps: role the task is assigned to (role-based assignment).</summary>
    public string? AssignedRole { get; set; }

    /// <summary>For manual steps: explicit user assignment (takes precedence over role).</summary>
    public Guid? AssignedUserId { get; set; }

    /// <summary>Whether the step must be completed (reserved; all v1 steps are required).</summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>Optional SLA: hours after activation before the task is considered due.</summary>
    public int? DueInHours { get; set; }

    /// <summary>For Notification steps: the notification template code to fire.</summary>
    public string? NotificationTemplateCode { get; set; }

    /// <summary>For Notification steps: role to notify (defaults to the change's context otherwise).</summary>
    public string? NotificationRole { get; set; }

    public string? Description { get; set; }
}
