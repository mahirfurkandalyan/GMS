namespace Gms.Api.Common;

/// <summary>
/// Release plan lifecycle statuses (System of Record). Created as Planned; then
/// Scheduled → InProgress (execution started) → Completed → Accepted (validation
/// passed), or Cancelled. The direct Scheduled → Completed edge is retained for the
/// legacy manual complete endpoint; the real deployment path goes through InProgress
/// (Execution domain), and Completed → Accepted is owned by the Validation domain.
/// Transitions owned by <see cref="Gms.Api.Domain.ReleasePlan"/>.
/// </summary>
public static class ReleaseStatuses
{
    public const string Planned = "Planned";
    public const string Scheduled = "Scheduled";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Accepted = "Accepted";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Planned, Scheduled, InProgress, Completed, Accepted, Cancelled
    };

    public static readonly IReadOnlyDictionary<string, HashSet<string>> Transitions = new Dictionary<string, HashSet<string>>
    {
        [Planned] = new() { Scheduled, Cancelled },
        [Scheduled] = new() { InProgress, Completed, Cancelled },
        [InProgress] = new() { Completed, Cancelled },
        [Completed] = new() { Accepted },
        [Accepted] = new(),
        [Cancelled] = new()
    };
}

/// <summary>Release types.</summary>
public static class ReleaseTypes
{
    public const string Major = "Major";
    public const string Minor = "Minor";
    public const string Patch = "Patch";
    public const string Hotfix = "Hotfix";
    public const string Emergency = "Emergency";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Major, Minor, Patch, Hotfix, Emergency
    };
}

/// <summary>Release risk levels (shared vocabulary with change risk).</summary>
public static class ReleaseRiskLevels
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
    public const string Critical = "Critical";
}

/// <summary>Release audit event type vocabulary.</summary>
public static class ReleaseAuditEventTypes
{
    public const string ReleaseCreated = "ReleaseCreated";
    public const string ReleaseUpdated = "ReleaseUpdated";
    public const string ReleaseScheduled = "ReleaseScheduled";
    public const string ReleaseExecutionStarted = "ReleaseExecutionStarted";
    public const string ReleaseCompleted = "ReleaseCompleted";
    public const string ReleaseValidationStarted = "ReleaseValidationStarted";
    public const string ReleaseValidationFailed = "ReleaseValidationFailed";
    public const string ReleaseAccepted = "ReleaseAccepted";
    public const string ReleaseCancelled = "ReleaseCancelled";
}
