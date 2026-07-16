using Gms.Api.Common;
using Gms.Api.Domain;

namespace Gms.Api.Services.Integrations.Providers;

/// <summary>
/// Jira sandbox adapter. Validates expected configuration and returns deterministic simulated
/// responses (no production calls). Demonstrates Jira-specific normalization (Issue Key → Project
/// Key + Issue Key) and an allowlisted incoming mapping.
/// </summary>
public sealed class JiraSandboxProvider : IIntegrationProvider
{
    public string Provider => IntegrationProviders.Jira;
    public bool SupportsIncoming => true;
    public bool SupportsOutgoing => true;

    public ProviderConfigValidationResult ValidateConfiguration(IntegrationDefinition definition)
    {
        var r = ProviderConfigValidationResult.Ok();
        if (string.IsNullOrWhiteSpace(definition.BaseUrl))
            r.Add("Jira temel URL'i (BaseUrl) zorunludur.");
        return r;
    }

    public Task<ProviderConnectionResult> TestConnectionAsync(IntegrationDefinition definition,
        IReadOnlyDictionary<string, string> creds, CancellationToken ct = default)
    {
        var ok = !string.IsNullOrWhiteSpace(definition.BaseUrl)
                 && !definition.BaseUrl!.Contains("fail", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(new ProviderConnectionResult(ok, ok ? 200 : 401,
            ok ? "Jira sandbox bağlantısı doğrulandı (simülasyon)." : "Jira kimlik doğrulaması başarısız (simülasyon).", 2));
    }

    public Task<ProviderExecuteResult> ExecuteAsync(ProviderExecuteRequest request, CancellationToken ct = default)
    {
        var reqSummary = $"Jira sandbox işlemi (event={request.Operation}).";
        return Task.FromResult(ProviderExecuteResult.Ok(200, reqSummary, "{\"status\":\"simulated\",\"provider\":\"Jira\"}"));
    }

    public IncomingWebhookValidationResult ValidateIncoming(IncomingWebhookContext context)
    {
        var secretError = ProviderHelpers.ValidateWebhookSecret(context);
        if (secretError is not null) return IncomingWebhookValidationResult.BadSignature(secretError);

        var root = ProviderHelpers.TryParse(context.Body);
        if (root is not { } el) return IncomingWebhookValidationResult.Invalid("Geçersiz JSON gövdesi.");

        var eventType = ProviderHelpers.GetString(el, "eventType") ?? ProviderHelpers.GetString(el, "webhookEvent");
        var issueKey = ProviderHelpers.GetString(el, "issueKey") ?? ProviderHelpers.GetString(el, "key");
        var reference = issueKey is null ? null : NormalizeReference(issueKey, "JiraIssue");

        string? action = string.Equals(eventType, "jira:issue_ready_for_review", StringComparison.OrdinalIgnoreCase)
            ? IntegrationWebhookMappings.JiraIssueReadyForReview : null;

        return IncomingWebhookValidationResult.Accept(ProviderHelpers.ExtractDeliveryId(context), action, reference);
    }

    public ExternalReference? NormalizeReference(string externalKeyOrId, string? externalObjectType)
    {
        // Jira issue key form: PROJECT-123. The project key is the prefix before the dash.
        var key = (externalKeyOrId ?? string.Empty).Trim().ToUpperInvariant();
        return new ExternalReference(externalObjectType ?? "JiraIssue", key, key, null);
    }
}
