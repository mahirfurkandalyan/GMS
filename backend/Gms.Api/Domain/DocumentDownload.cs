namespace Gms.Api.Domain;

/// <summary>Append-only record of a document download (who, which version, when, from where).</summary>
public class DocumentDownload
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }

    /// <summary>Downloaded version id (plain reference — no FK).</summary>
    public Guid VersionId { get; set; }

    public Guid DownloadedByUserId { get; set; }
    public DateTime DownloadedAt { get; set; }
    public string? IpAddress { get; set; }
}
