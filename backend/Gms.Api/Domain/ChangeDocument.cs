namespace Gms.Api.Domain;

/// <summary>
/// A supporting document reference attached to a change request. Metadata only —
/// no file storage in this sprint (prepares the document-domain integration).
/// </summary>
public class ChangeDocument
{
    public Guid Id { get; set; }

    public Guid ChangeRequestId { get; set; }
    public ChangeRequest? ChangeRequest { get; set; }

    public string DocumentType { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
