using Gms.Api.Domain;

namespace Gms.Api.Services.Integrations.Providers;

/// <summary>
/// A provider adapter behind a stable interface. Each external system (or protocol) is an adapter;
/// no domain implements raw HTTP integration itself. Providers are resolved by
/// <see cref="IntegrationDefinition.Provider"/> via <see cref="IIntegrationProviderResolver"/>.
/// Implementations must never log decrypted credentials or place secrets in returned summaries.
/// </summary>
public interface IIntegrationProvider
{
    /// <summary>Provider identifier (see <see cref="Gms.Api.Common.IntegrationProviders"/>).</summary>
    string Provider { get; }

    bool SupportsIncoming { get; }
    bool SupportsOutgoing { get; }

    /// <summary>Validates the definition's static configuration (called before activation).</summary>
    ProviderConfigValidationResult ValidateConfiguration(IntegrationDefinition definition);

    /// <summary>Tests connectivity/authentication. Decrypted credentials are provided; never logged.</summary>
    Task<ProviderConnectionResult> TestConnectionAsync(IntegrationDefinition definition,
        IReadOnlyDictionary<string, string> decryptedCredentials, CancellationToken ct = default);

    /// <summary>Executes an outgoing operation and returns a sanitized result.</summary>
    Task<ProviderExecuteResult> ExecuteAsync(ProviderExecuteRequest request, CancellationToken ct = default);

    /// <summary>Validates an incoming webhook (signature/secret, replay, mapping, normalization).</summary>
    IncomingWebhookValidationResult ValidateIncoming(IncomingWebhookContext context);

    /// <summary>Normalizes an external key/id into a structured reference (provider-specific).</summary>
    ExternalReference? NormalizeReference(string externalKeyOrId, string? externalObjectType);
}

/// <summary>Resolves the provider adapter for an integration definition.</summary>
public interface IIntegrationProviderResolver
{
    IIntegrationProvider Resolve(string provider);
    bool IsSupported(string provider);
}
