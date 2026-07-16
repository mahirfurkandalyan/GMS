namespace Gms.Api.Common;

/// <summary>
/// ValidationRun lifecycle statuses. Created → Running → Passed | Failed. A run may
/// be Cancelled from Created/Running (reserved; no cancel endpoint is exposed yet —
/// cancellation is a business decision, like execution rollback). Transitions owned
/// by <see cref="Gms.Api.Domain.ValidationRun"/>.
/// </summary>
public static class ValidationRunStatuses
{
    public const string Created = "Created";
    public const string Running = "Running";
    public const string Passed = "Passed";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Created, Running, Passed, Failed, Cancelled
    };

    public static readonly IReadOnlyDictionary<string, HashSet<string>> Transitions = new Dictionary<string, HashSet<string>>
    {
        [Created] = new() { Running, Cancelled },
        [Running] = new() { Passed, Failed, Cancelled },
        [Passed] = new(),
        [Failed] = new(),
        [Cancelled] = new()
    };
}

/// <summary>
/// ValidationCheck statuses. Waiting → Running → Passed | Failed; a not-yet-run check
/// may be Skipped when the run fails. Check ordering (single active check, no skipping
/// ahead) is enforced by the ValidationService, mirroring DeploymentStep/ApprovalStep.
/// </summary>
public static class ValidationCheckStatuses
{
    public const string Waiting = "Waiting";
    public const string Running = "Running";
    public const string Passed = "Passed";
    public const string Failed = "Failed";
    public const string Skipped = "Skipped";
}

/// <summary>Validation kinds (chosen at creation).</summary>
public static class ValidationTypes
{
    public const string Functional = "Functional";
    public const string Smoke = "Smoke";
    public const string Regression = "Regression";
    public const string UAT = "UAT";
    public const string Performance = "Performance";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Functional, Smoke, Regression, UAT, Performance
    };
}

/// <summary>Result vocabulary for a run's OverallResult.</summary>
public static class ValidationResults
{
    public const string Pending = "Pending";
    public const string Passed = "Passed";
    public const string Failed = "Failed";
}

/// <summary>ValidationEvent audit type vocabulary.</summary>
public static class ValidationEventTypes
{
    public const string ValidationCreated = "ValidationCreated";
    public const string ValidationStarted = "ValidationStarted";
    public const string CheckStarted = "CheckStarted";
    public const string CheckPassed = "CheckPassed";
    public const string CheckFailed = "CheckFailed";
    public const string CheckSkipped = "CheckSkipped";
    public const string ValidationPassed = "ValidationPassed";
    public const string ValidationFailed = "ValidationFailed";
}
