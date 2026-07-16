namespace Gms.Api.Domain;

/// <summary>
/// A recorded decision on an approval step. Captures the electronic-signature
/// placeholder (meaning + signer + timestamp) — the seed of Part 11 e-signatures.
/// </summary>
public class ApprovalDecision
{
    public Guid Id { get; set; }

    public Guid ApprovalRequestId { get; set; }
    public ApprovalRequest? ApprovalRequest { get; set; }

    public Guid ApprovalStepId { get; set; }
    public ApprovalStep? ApprovalStep { get; set; }

    /// <summary>Approved | Rejected | RevisionRequested.</summary>
    public string Decision { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;

    /// <summary>Electronic signature meaning placeholder (e.g. "I approve this change").</summary>
    public string SignatureMeaning { get; set; } = string.Empty;

    public Guid SignedByUserId { get; set; }
    public AppUser? SignedByUser { get; set; }

    public DateTime SignedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
