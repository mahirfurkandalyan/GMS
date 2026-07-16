using Gms.Api.Common;
using Gms.Api.Domain;

namespace Gms.Api.Services.Integrations.Providers;

/// <summary>
/// Outgoing webhook provider: delivers GMS event payloads to an external endpoint, optionally
/// signing the body with a configured webhook secret (HMAC-SHA256 in the signature header).
/// </summary>
public sealed class OutgoingWebhookIntegrationProvider : IIntegrationProvider
{
    private readonly IntegrationHttpClient _http;
    public OutgoingWebhookIntegrationProvider(IntegrationHttpClient http) => _http = http;

    public string Provider => IntegrationProviders.OutgoingWebhook;
    public bool SupportsIncoming => false;
    public bool SupportsOutgoing => true;

    public ProviderConfigValidationResult ValidateConfiguration(IntegrationDefinition definition)
    {
        var r = ProviderConfigValidationResult.Ok();
        if (string.IsNullOrWhiteSpace(definition.BaseUrl) || !Uri.TryCreate(definition.BaseUrl, UriKind.Absolute, out _))
            r.Add("Geçerli bir mutlak hedef BaseUrl zorunludur.");
        return r;
    }

    public async Task<ProviderConnectionResult> TestConnectionAsync(IntegrationDefinition definition,
        IReadOnlyDictionary<string, string> creds, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(definition.BaseUrl))
            return new ProviderConnectionResult(false, null, "Hedef URL tanımlı değil.", 0);
        var res = await _http.SendAsync("GET", definition.BaseUrl, null, null, 15, ct);
        return new ProviderConnectionResult(res.Success, res.StatusCode,
            res.Success ? "Hedef erişilebilir." : res.Error ?? "Hedefe ulaşılamadı.", (int)res.ElapsedMilliseconds);
    }

    public async Task<ProviderExecuteResult> ExecuteAsync(ProviderExecuteRequest request, CancellationToken ct = default)
    {
        var def = request.Definition;
        var url = ProviderHelpers.CombineUrl(def.BaseUrl, request.Endpoint?.RelativePath);
        var body = ProviderHelpers.SerializePayload(request.Payload);
        var headers = new Dictionary<string, string> { ["X-Gms-Event"] = request.Operation };
        if (request.DecryptedCredentials.TryGetValue(IntegrationCredentialKeys.WebhookSecret, out var secret) && !string.IsNullOrEmpty(secret))
            headers[ProviderHelpers.SignatureHeader] = ProviderHelpers.ComputeSignature(secret, body);
        var timeout = request.Endpoint?.TimeoutSeconds ?? 30;

        var reqSummary = $"POST {url} ({body.Length} bayt, event={request.Operation})";
        var res = await _http.SendAsync("POST", url, headers, body, timeout, ct);
        if (res.Success)
            return ProviderExecuteResult.Ok(res.StatusCode, reqSummary, res.ResponseSnippet);
        return ProviderExecuteResult.Fail(res.StatusCode, res.StatusCode is { } c ? $"HTTP_{c}" : "NETWORK",
            res.Error ?? "Teslimat başarısız.", res.IsTransient, reqSummary, res.ResponseSnippet);
    }

    public IncomingWebhookValidationResult ValidateIncoming(IncomingWebhookContext context)
        => IncomingWebhookValidationResult.Invalid("Bu sağlayıcı gelen webhook desteklemez.");

    public ExternalReference? NormalizeReference(string externalKeyOrId, string? externalObjectType)
        => new(externalObjectType ?? "Generic", externalKeyOrId, externalKeyOrId, null);
}
