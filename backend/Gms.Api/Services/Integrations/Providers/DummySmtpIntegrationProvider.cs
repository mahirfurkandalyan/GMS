using Gms.Api.Common;
using Gms.Api.Domain;

namespace Gms.Api.Services.Integrations.Providers;

/// <summary>
/// SMTP provider abstraction preparation. This sprint ships a deterministic dummy that validates
/// configuration and simulates a send (no real SMTP connection). A future real SMTP adapter can
/// replace the Execute/TestConnection bodies without changing the provider contract.
/// </summary>
public sealed class DummySmtpIntegrationProvider : IIntegrationProvider
{
    public string Provider => IntegrationProviders.Smtp;
    public bool SupportsIncoming => false;
    public bool SupportsOutgoing => true;

    public ProviderConfigValidationResult ValidateConfiguration(IntegrationDefinition definition)
    {
        var r = ProviderConfigValidationResult.Ok();
        if (string.IsNullOrWhiteSpace(definition.BaseUrl))
            r.Add("SMTP sunucu adresi (BaseUrl) zorunludur.");
        return r;
    }

    public Task<ProviderConnectionResult> TestConnectionAsync(IntegrationDefinition definition,
        IReadOnlyDictionary<string, string> creds, CancellationToken ct = default)
    {
        var ok = !string.IsNullOrWhiteSpace(definition.BaseUrl) &&
                 !definition.BaseUrl!.Contains("fail", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(new ProviderConnectionResult(ok, ok ? 250 : null,
            ok ? "SMTP el sıkışması simüle edildi (250)." : "SMTP sunucusuna ulaşılamadı (simülasyon).", 1));
    }

    public Task<ProviderExecuteResult> ExecuteAsync(ProviderExecuteRequest request, CancellationToken ct = default)
    {
        // Deterministic simulated send — never opens a real SMTP connection in this sprint.
        var reqSummary = $"SMTP gönderimi simüle edildi (event={request.Operation}).";
        return Task.FromResult(ProviderExecuteResult.Ok(250, reqSummary, "250 OK (simulated)"));
    }

    public IncomingWebhookValidationResult ValidateIncoming(IncomingWebhookContext context)
        => IncomingWebhookValidationResult.Invalid("SMTP sağlayıcısı gelen webhook desteklemez.");

    public ExternalReference? NormalizeReference(string externalKeyOrId, string? externalObjectType) => null;
}
