namespace Gms.Api.Domain;

/// <summary>
/// One immutable version of a document's file. Every upload creates a new version and
/// increments <see cref="VersionNumber"/>; old versions stay downloadable and are never
/// overwritten. Physical storage details live only in <see cref="StoragePath"/> (never
/// exposed via DTOs).
/// </summary>
public class DocumentVersion
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }

    public int VersionNumber { get; set; }

    /// <summary>Storage-relative path (opaque to callers; resolved by IFileStorage).</summary>
    public string StoragePath { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;

    public Guid UploadedByUserId { get; set; }
    public AppUser? UploadedByUser { get; set; }
    public DateTime UploadedAt { get; set; }

    public string Comment { get; set; } = string.Empty;
}
