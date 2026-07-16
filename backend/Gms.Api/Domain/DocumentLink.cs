namespace Gms.Api.Domain;

/// <summary>
/// Links a document to a business object (Change, Approval, Release, Deployment,
/// Validation, or a future domain). One document may link to many objects; one object
/// may have many documents. A unique index on (DocumentId, ObjectType, ObjectId)
/// prevents duplicate links.
/// </summary>
public class DocumentLink
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }

    /// <summary>See <see cref="Gms.Api.Common.DocumentObjectTypes"/>.</summary>
    public string ObjectType { get; set; } = string.Empty;
    public Guid ObjectId { get; set; }

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
