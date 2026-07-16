using Gms.Api.Common;
using Gms.Api.Domain;

namespace Gms.Api.Services.Integrations.Providers;

/// <summary>
/// Generic incoming webhook provider: validates the shared secret / HMAC signature and delivery
/// id (replay/duplicate detection), then accepts the delivery for synchronous foundation
/// processing. Does not perform outgoing requests.
/// </summary>
public sealed class IncomingWebhookIntegrationProvider : IIntegrationProvider
{
    public string Provider => IntegrationProviders.IncomingWebhook;
    public bool SupportsIncoming => true;
    public bool SupportsOutgoing => false;

    public ProviderConfigValidationResult ValidateConfiguration(IntegrationDefinition definition)
    {
        var r = ProviderConfigValidationResult.Ok();
        if (definition.AuthenticationType != IntegrationAuthTypes.WebhookSecret)
            r.Add("Gelen webhook için kimlik doğrulama türü 'WebhookSecret' olmalıdır.");
        return r;
    }

    public Task<ProviderConnectionResult> TestConnectionAsync(IntegrationDefinition definition,
        IReadOnlyDictionary<string, string> creds, CancellationToken ct = default)
    {
        var ok = creds.ContainsKey(IntegrationCredentialKeys.WebhookSecret);
        return Task.FromResult(new ProviderConnectionResult(ok, null,
            ok ? "Gelen webhook uç noktası hazır." : "Webhook gizli anahtarı eksik.", 0));
    }

    public Task<ProviderExecuteResult> ExecuteAsync(ProviderExecuteRequest request, CancellationToken ct = default)
        => Task.FromResult(ProviderExecuteResult.Fail(null, "UNSUPPORTED", "Bu sağlayıcı giden çağrı yapmaz.", false));

    public IncomingWebhookValidationResult ValidateIncoming(IncomingWebhookContext context)
    {
        var secretError = ProviderHelpers.ValidateWebhookSecret(context);
        if (secretError is not null) return IncomingWebhookValidationResult.BadSignature(secretError);
        if (ProviderHelpers.TryParse(context.Body) is null) return IncomingWebhookValidationResult.Invalid("Geçersiz JSON gövdesi.");
        return IncomingWebhookValidationResult.Accept(ProviderHelpers.ExtractDeliveryId(context), null, null);
    }

    public ExternalReference? NormalizeReference(string externalKeyOrId, string? externalObjectType)
        => new(externalObjectType ?? "Generic", externalKeyOrId, externalKeyOrId, null);
}
