namespace Gms.Api.Domain;

/// <summary>
/// A protected secret/identifier belonging to an integration. The raw value is NEVER stored:
/// only <see cref="EncryptedValue"/> (Data Protection output) is persisted, and only
/// <see cref="MaskedValue"/> is exposed through DTOs. Decryption happens exclusively inside
/// provider execution.
/// </summary>
public class IntegrationCredential
{
    public Guid Id { get; set; }

    public Guid IntegrationDefinitionId { get; set; }
    public IntegrationDefinition? IntegrationDefinition { get; set; }

    /// <summary>Credential type (mirrors the auth type it serves).</summary>
    public string CredentialType { get; set; } = string.Empty;

    /// <summary>Logical key name (see <see cref="Gms.Api.Common.IntegrationCredentialKeys"/>).</summary>
    public string KeyName { get; set; } = string.Empty;

    /// <summary>Protected (encrypted) secret value. Never returned through a DTO.</summary>
    public string EncryptedValue { get; set; } = string.Empty;

    /// <summary>Non-reversible display mask (safe to expose/audit).</summary>
    public string MaskedValue { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? RotatedAt { get; set; }
}
