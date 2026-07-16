using Gms.Api.Common;

namespace Gms.Api.Services.Integrations.Providers;

/// <summary>
/// Resolves a provider adapter by its identifier from the DI-registered set. Avoids scattered
/// switch statements: new providers are added simply by registering another
/// <see cref="IIntegrationProvider"/>.
/// </summary>
public sealed class IntegrationProviderResolver : IIntegrationProviderResolver
{
    private readonly IReadOnlyDictionary<string, IIntegrationProvider> _providers;

    public IntegrationProviderResolver(IEnumerable<IIntegrationProvider> providers)
        => _providers = providers.ToDictionary(p => p.Provider, StringComparer.OrdinalIgnoreCase);

    public bool IsSupported(string provider) => _providers.ContainsKey(provider);

    public IIntegrationProvider Resolve(string provider)
        => _providers.TryGetValue(provider, out var p)
            ? p
            : throw new IntegrationValidationException($"'{provider}' sağlayıcısı bu sürümde desteklenmiyor.");
}
