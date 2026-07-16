namespace Gms.Api.Contracts;

/// <summary>Summary row for the documents list.</summary>
public class DocumentListDto
{
    public Guid Id { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string OwnerUserName { get; set; } = string.Empty;
    public int VersionCount { get; set; }
    public int CurrentVersionNumber { get; set; }
    public long CurrentSizeBytes { get; set; }
    public int LinkCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Full document detail, including versions and links (never physical paths).</summary>
public class DocumentDetailDto
{
    public Guid Id { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid OwnerUserId { get; set; }
    public string OwnerUserName { get; set; } = string.Empty;
    public Guid? CurrentVersionId { get; set; }
    public int CurrentVersionNumber { get; set; }
    public string HashAlgorithm { get; set; } = string.Empty;
    public string CurrentHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Base64 concurrency token; echo it back on update to detect conflicts (409).</summary>
    public string RowVersion { get; set; } = string.Empty;

    public List<DocumentVersionDto> Versions { get; set; } = new();
    public List<DocumentLinkDto> Links { get; set; } = new();
}

/// <summary>One immutable version. Physical storage details are intentionally omitted.</summary>
public class DocumentVersionDto
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public Guid UploadedByUserId { get; set; }
    public string UploadedByUserName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public class DocumentLinkDto
{
    public Guid Id { get; set; }
    public string ObjectType { get; set; } = string.Empty;
    public Guid ObjectId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DocumentAuditEventDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DocumentDownloadDto
{
    public Guid Id { get; set; }
    public Guid VersionId { get; set; }
    public Guid DownloadedByUserId { get; set; }
    public string DownloadedByUserName { get; set; } = string.Empty;
    public DateTime DownloadedAt { get; set; }
    public string? IpAddress { get; set; }
}

/* ── request DTOs ── */

public class CreateDocumentDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class UpdateDocumentDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }

    /// <summary>Optional base64 concurrency token; a mismatch yields 409 Conflict.</summary>
    public string? RowVersion { get; set; }
}

public class AttachDocumentDto
{
    public string ObjectType { get; set; } = string.Empty;
    public Guid ObjectId { get; set; }
}
