namespace Gms.Api.Common;

/// <summary>
/// DeploymentRun lifecycle statuses. Created → Running → Completed (success),
/// Running → Failed (a step failed) → RolledBack (rollback executed). Transitions
/// owned by <see cref="Gms.Api.Domain.DeploymentRun"/>.
/// </summary>
public static class DeploymentRunStatuses
{
    public const string Created = "Created";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string RolledBack = "RolledBack";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Created, Running, Completed, Failed, RolledBack
    };

    public static readonly IReadOnlyDictionary<string, HashSet<string>> Transitions = new Dictionary<string, HashSet<string>>
    {
        [Created] = new() { Running },
        [Running] = new() { Completed, Failed },
        [Failed] = new() { RolledBack },
        [Completed] = new(),
        [RolledBack] = new()
    };
}

/// <summary>
/// DeploymentStep statuses. Waiting → Running → Completed | Failed; a not-yet-run
/// step may be Skipped during rollback; a Failed step becomes RolledBack when the
/// run is rolled back. Step ordering/guards are enforced by the ExecutionService
/// (single active step), mirroring how ApprovalStep is handled.
/// </summary>
public static class DeploymentStepStatuses
{
    public const string Waiting = "Waiting";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Skipped = "Skipped";
    public const string RolledBack = "RolledBack";
}

/// <summary>Result vocabulary for a run's OverallResult and a step's ExecutionResult.</summary>
public static class DeploymentResults
{
    public const string Pending = "Pending";
    public const string Success = "Success";
    public const string Failure = "Failure";
    public const string RolledBack = "RolledBack";
}

/// <summary>DeploymentEvent audit type vocabulary.</summary>
public static class DeploymentEventTypes
{
    public const string ExecutionCreated = "ExecutionCreated";
    public const string ExecutionStarted = "ExecutionStarted";
    public const string StepStarted = "StepStarted";
    public const string StepCompleted = "StepCompleted";
    public const string StepFailed = "StepFailed";
    public const string StepSkipped = "StepSkipped";
    public const string StepRolledBack = "StepRolledBack";
    public const string ExecutionCompleted = "ExecutionCompleted";
    public const string ExecutionFailed = "ExecutionFailed";
    public const string RollbackStarted = "RollbackStarted";
    public const string ExecutionRolledBack = "ExecutionRolledBack";
}
