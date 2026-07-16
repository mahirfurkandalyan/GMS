namespace Gms.Api.Domain;

/// <summary>A supporting document reference attached to a release plan (metadata only).</summary>
public class ReleaseDocument
{
    public Guid Id { get; set; }

    public Guid ReleasePlanId { get; set; }
    public ReleasePlan? ReleasePlan { get; set; }

    public string DocumentType { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
