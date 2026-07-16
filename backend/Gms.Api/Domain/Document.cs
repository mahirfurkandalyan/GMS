using Gms.Api.Common;

namespace Gms.Api.Domain;

/// <summary>
/// The single, reusable document aggregate for the whole GMS platform. Every business
/// module (Change, Approval, Release, Execution, Validation, …) links to documents via
/// <see cref="DocumentLink"/> — there is no other document implementation. Files are
/// versioned and immutable; the aggregate tracks the current version + integrity hash.
/// </summary>
public class Document
{
    public Guid Id { get; set; }

    /// <summary>Human-readable number, e.g. DOC-2026-000001.</summary>
    public string DocumentNo { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    /// <summary>Draft | Active | Archived | Deleted.</summary>
    public string Status { get; set; } = DocumentStatuses.Draft;

    public Guid OwnerUserId { get; set; }
    public AppUser? OwnerUser { get; set; }

    /// <summary>Id of the latest version (plain reference — no FK, avoids a cyclic cascade).</summary>
    public Guid? CurrentVersionId { get; set; }

    public string HashAlgorithm { get; set; } = DocumentFileRules.HashAlgorithm;

    /// <summary>SHA-256 hash of the current version's file (integrity anchor).</summary>
    public string CurrentHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Relationships
    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    public ICollection<DocumentLink> Links { get; set; } = new List<DocumentLink>();
    public ICollection<DocumentAuditEvent> AuditEvents { get; set; } = new List<DocumentAuditEvent>();
    public ICollection<DocumentDownload> Downloads { get; set; } = new List<DocumentDownload>();

    /// <summary>Guarded status transition — the aggregate owns its own lifecycle.</summary>
    public void TransitionTo(string target)
    {
        StatusTransition.Ensure(DocumentStatuses.Transitions, nameof(Document), Status, target);
        Status = target;
    }
}
