namespace Gms.Api.Common;

/// <summary>Valid change classes.</summary>
public static class ChangeClasses
{
    public const string Standard = "Standard";
    public const string Normal = "Normal";
    public const string Emergency = "Emergency";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Standard, Normal, Emergency };
}

/// <summary>Valid change (technical) types.</summary>
public static class ChangeTypes
{
    public const string ApplicationDeployment = "ApplicationDeployment";
    public const string DatabaseSchemaChange = "DatabaseSchemaChange";
    public const string SqlDataFix = "SqlDataFix";
    public const string StoredProcedureFunctionChange = "StoredProcedureFunctionChange";
    public const string ApiChange = "ApiChange";
    public const string ConfigurationChange = "ConfigurationChange";
    public const string InfrastructureChange = "InfrastructureChange";
    public const string IntegrationChange = "IntegrationChange";
    public const string DocumentSopChange = "DocumentSopChange";
    public const string Other = "Other";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        ApplicationDeployment, DatabaseSchemaChange, SqlDataFix, StoredProcedureFunctionChange,
        ApiChange, ConfigurationChange, InfrastructureChange, IntegrationChange, DocumentSopChange, Other
    };

    /// <summary>Types that touch SQL/database objects and therefore require a rollback script.</summary>
    public static readonly IReadOnlySet<string> SqlRelated = new HashSet<string>
    {
        DatabaseSchemaChange, SqlDataFix, StoredProcedureFunctionChange
    };
}

/// <summary>Valid change priorities.</summary>
public static class ChangePriorities
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
    public const string Critical = "Critical";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Low, Medium, High, Critical };
}

/// <summary>
/// Change lifecycle statuses. Implemented is the terminal success state; a change
/// reaches it when its release completes. ('Completed' was removed as an unreachable
/// state during hardening — a future domain can reintroduce it if genuinely needed.)
/// Allowed transitions are owned by <see cref="Gms.Api.Domain.ChangeRequest"/>.
/// </summary>
public static class ChangeStatuses
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string UnderReview = "UnderReview";
    public const string Approved = "Approved";
    public const string Scheduled = "Scheduled";
    public const string Implemented = "Implemented";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Draft, Submitted, UnderReview, Approved, Scheduled, Implemented, Cancelled
    };

    /// <summary>Allowed lifecycle transitions (source → targets).</summary>
    public static readonly IReadOnlyDictionary<string, HashSet<string>> Transitions = new Dictionary<string, HashSet<string>>
    {
        [Draft] = new() { Submitted, Cancelled },
        [Submitted] = new() { UnderReview, Cancelled },
        [UnderReview] = new() { Approved, Submitted, Draft, Cancelled },
        [Approved] = new() { Scheduled, Cancelled },
        [Scheduled] = new() { Implemented, Approved }, // released changes leave only via the release
        [Implemented] = new(),
        [Cancelled] = new()
    };
}

/// <summary>Risk levels (also used for asset criticality vocabulary).</summary>
public static class ChangeRiskLevels
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
    public const string Critical = "Critical";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Low, Medium, High, Critical };
}

/// <summary>Change audit event type vocabulary.</summary>
public static class ChangeAuditEventTypes
{
    public const string ChangeCreated = "ChangeCreated";
    public const string ChangeUpdated = "ChangeUpdated";
    public const string ChangeSubmitted = "ChangeSubmitted";
    public const string ChangeCancelled = "ChangeCancelled";
    public const string RevisionCreated = "RevisionCreated";
    // Approval flow integration
    public const string ApprovalRequested = "ApprovalRequested";
    public const string ChangeApproved = "ChangeApproved";
    public const string ChangeApprovalRejected = "ChangeApprovalRejected";
    public const string ChangeRevisionRequested = "ChangeRevisionRequested";
    // Release planning integration
    public const string ChangeScheduled = "ChangeScheduled";
    public const string ChangeImplemented = "ChangeImplemented";
    public const string ChangeUnscheduled = "ChangeUnscheduled";
}
