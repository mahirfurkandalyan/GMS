using Gms.Api.Common;
using Gms.Api.Domain;

namespace Gms.Api.Services.Integrations.Providers;

/// <summary>Generic REST provider: real outgoing HTTP requests with configurable auth.</summary>
public sealed class GenericRestIntegrationProvider : IIntegrationProvider
{
    private readonly IntegrationHttpClient _http;
    public GenericRestIntegrationProvider(IntegrationHttpClient http) => _http = http;

    public string Provider => IntegrationProviders.GenericRest;
    public bool SupportsIncoming => false;
    public bool SupportsOutgoing => true;

    public ProviderConfigValidationResult ValidateConfiguration(IntegrationDefinition definition)
    {
        var r = ProviderConfigValidationResult.Ok();
        if (string.IsNullOrWhiteSpace(definition.BaseUrl) || !Uri.TryCreate(definition.BaseUrl, UriKind.Absolute, out _))
            r.Add("Geçerli bir mutlak BaseUrl zorunludur.");
        return r;
    }

    public async Task<ProviderConnectionResult> TestConnectionAsync(IntegrationDefinition definition,
        IReadOnlyDictionary<string, string> creds, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(definition.BaseUrl))
            return new ProviderConnectionResult(false, null, "BaseUrl tanımlı değil.", 0);

        var headers = ProviderHelpers.BuildAuthHeaders(definition, creds);
        var res = await _http.SendAsync("GET", definition.BaseUrl, headers, null, 15, ct);
        return new ProviderConnectionResult(res.Success, res.StatusCode,
            res.Success ? "Bağlantı başarılı." : res.Error ?? "Bağlantı başarısız.", (int)res.ElapsedMilliseconds);
    }

    public async Task<ProviderExecuteResult> ExecuteAsync(ProviderExecuteRequest request, CancellationToken ct = default)
    {
        var def = request.Definition;
        var method = request.Endpoint?.HttpMethod ?? "POST";
        var url = ProviderHelpers.CombineUrl(def.BaseUrl, request.Endpoint?.RelativePath);
        var body = ProviderHelpers.SerializePayload(request.Payload);
        var headers = ProviderHelpers.BuildAuthHeaders(def, request.DecryptedCredentials);
        var timeout = request.Endpoint?.TimeoutSeconds ?? 30;

        var reqSummary = $"{method} {url} ({body.Length} bayt)";
        var res = await _http.SendAsync(method, url, headers, body, timeout, ct);
        if (res.Success)
            return ProviderExecuteResult.Ok(res.StatusCode, reqSummary, res.ResponseSnippet);
        return ProviderExecuteResult.Fail(res.StatusCode, res.StatusCode is { } c ? $"HTTP_{c}" : "NETWORK",
            res.Error ?? "İstek başarısız.", res.IsTransient, reqSummary, res.ResponseSnippet);
    }

    public IncomingWebhookValidationResult ValidateIncoming(IncomingWebhookContext context)
        => IncomingWebhookValidationResult.Invalid("Bu sağlayıcı gelen webhook desteklemez.");

    public ExternalReference? NormalizeReference(string externalKeyOrId, string? externalObjectType)
        => new(externalObjectType ?? "Generic", externalKeyOrId, externalKeyOrId, null);
}
