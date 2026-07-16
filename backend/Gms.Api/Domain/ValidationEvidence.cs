namespace Gms.Api.Domain;

/// <summary>
/// A piece of evidence attached to a validation run (screenshot, log, report, etc.).
/// PoC scope: metadata only — no binary file storage/upload.
/// </summary>
public class ValidationEvidence
{
    public Guid Id { get; set; }

    public Guid ValidationRunId { get; set; }
    public ValidationRun? ValidationRun { get; set; }

    public string EvidenceType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
