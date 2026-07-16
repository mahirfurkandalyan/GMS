namespace Gms.Api.Common;

/// <summary>WorkflowDefinition statuses. Archived is terminal.</summary>
public static class WorkflowDefinitionStatuses
{
    public const string Draft = "Draft";
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Archived = "Archived";

    public static readonly IReadOnlyDictionary<string, HashSet<string>> Transitions = new Dictionary<string, HashSet<string>>
    {
        [Draft] = new() { Active, Archived },
        [Active] = new() { Inactive, Archived },
        [Inactive] = new() { Active, Archived },
        [Archived] = new()
    };
}

/// <summary>WorkflowVersion statuses. Published versions are immutable.</summary>
public static class WorkflowVersionStatuses
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Retired = "Retired";
}

/// <summary>
/// WorkflowInstance statuses. Terminal states: Completed (reached End → object approved),
/// Rejected (an approval step rejected → object sent back), Cancelled (manually cancelled),
/// Failed (engine error / step limit exceeded).
/// </summary>
public static class WorkflowInstanceStatuses
{
    public const string Created = "Created";
    public const string Running = "Running";
    public const string Waiting = "Waiting";
    public const string Completed = "Completed";
    public const string Rejected = "Rejected";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyDictionary<string, HashSet<string>> Transitions = new Dictionary<string, HashSet<string>>
    {
        [Created] = new() { Running, Cancelled },
        [Running] = new() { Waiting, Completed, Rejected, Failed, Cancelled },
        [Waiting] = new() { Running, Completed, Rejected, Failed, Cancelled },
        [Completed] = new(),
        [Rejected] = new(),
        [Failed] = new(),
        [Cancelled] = new()
    };
}

/// <summary>WorkflowStepInstance statuses.</summary>
public static class WorkflowStepStatuses
{
    public const string Waiting = "Waiting";
    public const string Active = "Active";
    public const string Completed = "Completed";
    public const string Rejected = "Rejected";
    public const string Skipped = "Skipped";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}

/// <summary>Workflow step types.</summary>
public static class WorkflowStepTypes
{
    public const string Start = "Start";
    public const string ManualTask = "ManualTask";
    public const string Approval = "Approval";
    public const string Condition = "Condition";
    public const string Notification = "Notification";
    public const string End = "End";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Start, ManualTask, Approval, Condition, Notification, End };

    /// <summary>Steps that require a human action (pause the workflow).</summary>
    public static readonly IReadOnlySet<string> Manual = new HashSet<string> { ManualTask, Approval };

    /// <summary>Steps the engine processes automatically.</summary>
    public static readonly IReadOnlySet<string> Automatic = new HashSet<string> { Start, Condition, Notification, End };
}

/// <summary>Transition condition types.</summary>
public static class WorkflowConditionTypes
{
    public const string Always = "Always";
    public const string ObjectField = "ObjectField";
    public const string RiskLevel = "RiskLevel";
    public const string Status = "Status";
    public const string Boolean = "Boolean";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Always, ObjectField, RiskLevel, Status, Boolean };
}

/// <summary>Condition operators.</summary>
public static class WorkflowOperators
{
    public new const string Equals = "Equals";
    public const string NotEquals = "NotEquals";
    public const string GreaterThan = "GreaterThan";
    public const string GreaterThanOrEqual = "GreaterThanOrEqual";
    public const string LessThan = "LessThan";
    public const string LessThanOrEqual = "LessThanOrEqual";
    public const string Contains = "Contains";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Equals, NotEquals, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual, Contains
    };
}

/// <summary>
/// Allowlisted condition fields for Change workflows. Conditions may reference ONLY these
/// fields — no dynamic access, reflection or user scripts. Unknown fields are rejected at publish.
/// </summary>
public static class WorkflowChangeFields
{
    public const string ChangeClass = "changeClass";
    public const string ChangeType = "changeType";
    public const string Priority = "priority";
    public const string RiskLevel = "riskLevel";
    public const string RiskScore = "riskScore";
    public const string EnvironmentName = "environmentName";
    public const string Status = "status";
    public const string ReadinessScore = "readinessScore";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ChangeClass, ChangeType, Priority, RiskLevel, RiskScore, EnvironmentName, Status, ReadinessScore
    };

    /// <summary>Numeric fields (comparison operators use numeric semantics).</summary>
    public static readonly IReadOnlySet<string> Numeric = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        RiskScore, ReadinessScore
    };
}

/// <summary>Trigger object types + events (what starts a workflow).</summary>
public static class WorkflowTriggers
{
    public const string ChangeRequestObject = "ChangeRequest";
    public const string ChangeSubmittedEvent = "ChangeSubmitted";
}

/// <summary>Workflow categories.</summary>
public static class WorkflowCategories
{
    public const string ChangeManagement = "ChangeManagement";
}

/// <summary>WorkflowEvent audit type vocabulary.</summary>
public static class WorkflowEventTypes
{
    public const string WorkflowCreated = "WorkflowCreated";
    public const string WorkflowStarted = "WorkflowStarted";
    public const string StepActivated = "StepActivated";
    public const string StepCompleted = "StepCompleted";
    public const string StepRejected = "StepRejected";
    public const string StepSkipped = "StepSkipped";
    public const string ConditionEvaluated = "ConditionEvaluated";
    public const string NotificationTriggered = "NotificationTriggered";
    public const string WorkflowCompleted = "WorkflowCompleted";
    public const string WorkflowFailed = "WorkflowFailed";
    public const string WorkflowCancelled = "WorkflowCancelled";
    public const string WorkflowPaused = "WorkflowPaused";
    public const string WorkflowResumed = "WorkflowResumed";
    public const string SlaReminderSent = "SlaReminderSent";
}

/// <summary>Step completion result vocabulary.</summary>
public static class WorkflowStepResults
{
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Completed = "Completed";
    public const string Auto = "Auto";
}
