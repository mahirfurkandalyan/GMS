using Microsoft.AspNetCore.DataProtection;

namespace Gms.Api.Services.Integrations;

/// <summary>
/// Abstraction over secret protection. Raw secret values are NEVER stored, returned or logged.
/// Only the protected (encrypted) form is persisted; only a masked form is exposed through DTOs;
/// decryption happens exclusively inside provider execution.
/// </summary>
public interface ISecretProtector
{
    /// <summary>Encrypts a raw secret for storage. Returns an opaque protected string.</summary>
    string Protect(string value);

    /// <summary>Decrypts a protected value. Only call inside provider execution; never log the result.</summary>
    string Unprotect(string encryptedValue);

    /// <summary>Produces a non-reversible display mask (e.g. "abcd••••wxyz") for DTOs/audit.</summary>
    string Mask(string value);
}

/// <summary>
/// ASP.NET Core Data Protection implementation. Uses a dedicated protector purpose so keys are
/// isolated from other subsystems. NOTE (production): Data Protection keys must be persisted to a
/// durable, shared key ring (e.g. a mounted volume, database or key vault) and encrypted at rest;
/// otherwise protected credentials become undecryptable after a key-ring reset or on multi-node
/// deployments. This is documented in the sprint report.
/// </summary>
public sealed class DataProtectionSecretProtector : ISecretProtector
{
    private const string Purpose = "Gms.Integration.Credentials.v1";
    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider provider)
        => _protector = provider.CreateProtector(Purpose);

    public string Protect(string value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        return _protector.Protect(value);
    }

    public string Unprotect(string encryptedValue)
    {
        if (encryptedValue is null) throw new ArgumentNullException(nameof(encryptedValue));
        return _protector.Unprotect(encryptedValue);
    }

    public string Mask(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var v = value.Trim();
        if (v.Length <= 4) return new string('•', v.Length);
        if (v.Length <= 8) return v[..1] + new string('•', v.Length - 1);
        return v[..2] + new string('•', Math.Min(8, v.Length - 4)) + v[^2..];
    }
}
