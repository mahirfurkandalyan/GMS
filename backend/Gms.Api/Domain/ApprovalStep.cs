namespace Gms.Api.Domain;

/// <summary>
/// A single step in an approval chain. Exactly one step is Active at a time;
/// upstream steps are Approved, downstream steps are Waiting.
/// </summary>
public class ApprovalStep
{
    public Guid Id { get; set; }

    public Guid ApprovalRequestId { get; set; }
    public ApprovalRequest? ApprovalRequest { get; set; }

    public int StepNo { get; set; }
    public string StepName { get; set; } = string.Empty;

    /// <summary>Architect | QA | ReleaseManager | Admin.</summary>
    public string ApproverRole { get; set; } = string.Empty;

    /// <summary>Resolved approver; null when no user exists for the role yet.</summary>
    public Guid? ApproverUserId { get; set; }
    public AppUser? ApproverUser { get; set; }

    /// <summary>Waiting | Active | Approved | Rejected | Skipped | Cancelled.</summary>
    public string Status { get; set; } = string.Empty;

    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
