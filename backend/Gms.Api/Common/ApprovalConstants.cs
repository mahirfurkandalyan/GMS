namespace Gms.Api.Common;

/// <summary>Object types an approval request can target.</summary>
public static class ApprovalRelatedObjectTypes
{
    public const string ChangeRequest = "ChangeRequest";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { ChangeRequest };
}

/// <summary>
/// Approval request lifecycle statuses. Created directly as InProgress; ends in
/// Approved, Rejected or Cancelled. ('Draft', 'Pending' and 'Expired' were removed
/// as unreachable states during hardening.) Transitions owned by
/// <see cref="Gms.Api.Domain.ApprovalRequest"/>.
/// </summary>
public static class ApprovalStatuses
{
    public const string InProgress = "InProgress";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        InProgress, Approved, Rejected, Cancelled
    };

    public static readonly IReadOnlyDictionary<string, HashSet<string>> Transitions = new Dictionary<string, HashSet<string>>
    {
        [InProgress] = new() { Approved, Rejected, Cancelled },
        [Approved] = new(),
        [Rejected] = new(),
        [Cancelled] = new()
    };
}

/// <summary>Approval step statuses.</summary>
public static class ApprovalStepStatuses
{
    public const string Waiting = "Waiting";
    public const string Active = "Active";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Skipped = "Skipped";
    public const string Cancelled = "Cancelled";
}

/// <summary>Approval decision values.</summary>
public static class ApprovalDecisions
{
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string RevisionRequested = "RevisionRequested";
}

/// <summary>Approval audit event type vocabulary.</summary>
public static class ApprovalEventTypes
{
    public const string ApprovalCreated = "ApprovalCreated";
    public const string StepActivated = "StepActivated";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string RevisionRequested = "RevisionRequested";
    public const string ApprovalCompleted = "ApprovalCompleted";
    public const string ApprovalCancelled = "ApprovalCancelled";
}

/// <summary>Approver roles used to build the approval chain.</summary>
public static class ApproverRoles
{
    public const string Architect = "Architect";
    public const string QA = "QA";
    public const string ReleaseManager = "ReleaseManager";
    public const string Admin = "Admin";
}
