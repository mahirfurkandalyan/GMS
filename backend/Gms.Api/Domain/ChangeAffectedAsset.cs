namespace Gms.Api.Domain;

/// <summary>
/// An asset affected by a change request. Denormalized snapshot (AssetName/Type)
/// for this sprint; a real Asset FK relationship comes in a later asset-domain sprint.
/// </summary>
public class ChangeAffectedAsset
{
    public Guid Id { get; set; }

    public Guid ChangeRequestId { get; set; }
    public ChangeRequest? ChangeRequest { get; set; }

    public string AssetType { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;

    /// <summary>Low | Medium | High | Critical.</summary>
    public string Criticality { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
