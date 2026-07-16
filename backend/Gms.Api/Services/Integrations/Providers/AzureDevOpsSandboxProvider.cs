using Gms.Api.Common;
using Gms.Api.Domain;

namespace Gms.Api.Services.Integrations.Providers;

/// <summary>
/// Azure DevOps sandbox adapter. Validates expected configuration and returns deterministic
/// simulated responses (no production calls). Demonstrates provider-specific normalization of
/// Work Item / Pipeline Run / Pull Request references and an allowlisted incoming mapping.
/// </summary>
public sealed class AzureDevOpsSandboxProvider : IIntegrationProvider
{
    public string Provider => IntegrationProviders.AzureDevOps;
    public bool SupportsIncoming => true;
    public bool SupportsOutgoing => true;

    public ProviderConfigValidationResult ValidateConfiguration(IntegrationDefinition definition)
    {
        var r = ProviderConfigValidationResult.Ok();
        if (string.IsNullOrWhiteSpace(definition.BaseUrl))
            r.Add("Azure DevOps organizasyon URL'i (BaseUrl) zorunludur.");
        return r;
    }

    public Task<ProviderConnectionResult> TestConnectionAsync(IntegrationDefinition definition,
        IReadOnlyDictionary<string, string> creds, CancellationToken ct = default)
    {
        var ok = !string.IsNullOrWhiteSpace(definition.BaseUrl)
                 && !definition.BaseUrl!.Contains("fail", StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(new ProviderConnectionResult(ok, ok ? 200 : 401,
            ok ? "Azure DevOps sandbox bağlantısı doğrulandı (simülasyon)." : "Azure DevOps kimlik doğrulaması başarısız (simülasyon).", 2));
    }

    public Task<ProviderExecuteResult> ExecuteAsync(ProviderExecuteRequest request, CancellationToken ct = default)
    {
        var reqSummary = $"Azure DevOps sandbox işlemi (event={request.Operation}).";
        return Task.FromResult(ProviderExecuteResult.Ok(200, reqSummary, "{\"status\":\"simulated\",\"provider\":\"AzureDevOps\"}"));
    }

    public IncomingWebhookValidationResult ValidateIncoming(IncomingWebhookContext context)
    {
        var secretError = ProviderHelpers.ValidateWebhookSecret(context);
        if (secretError is not null) return IncomingWebhookValidationResult.BadSignature(secretError);

        var root = ProviderHelpers.TryParse(context.Body);
        if (root is not { } el) return IncomingWebhookValidationResult.Invalid("Geçersiz JSON gövdesi.");

        var eventType = ProviderHelpers.GetString(el, "eventType");
        var workItemId = ProviderHelpers.GetString(el, "workItemId") ?? ProviderHelpers.GetString(el, "id");
        var reference = workItemId is null ? null : NormalizeReference(workItemId, "AzureDevOpsWorkItem");

        // Only an allowlisted event maps to an action; anything else is accepted-but-unmapped.
        string? action = string.Equals(eventType, "workitem.ready-for-release", StringComparison.OrdinalIgnoreCase)
            ? IntegrationWebhookMappings.AzureDevOpsWorkItemReadyForRelease : null;

        return IncomingWebhookValidationResult.Accept(ProviderHelpers.ExtractDeliveryId(context), action, reference);
    }

    public ExternalReference? NormalizeReference(string externalKeyOrId, string? externalObjectType)
    {
        var v = (externalKeyOrId ?? string.Empty).Trim();
        if (v.StartsWith("PR-", StringComparison.OrdinalIgnoreCase))
            return new ExternalReference("AzureDevOpsPullRequest", v[3..], v, null);
        if (v.StartsWith("RUN-", StringComparison.OrdinalIgnoreCase))
            return new ExternalReference("AzureDevOpsPipelineRun", v[4..], v, null);
        // Bare numeric or WI-prefixed → work item.
        var id = v.StartsWith("WI-", StringComparison.OrdinalIgnoreCase) ? v[3..] : v;
        return new ExternalReference(externalObjectType ?? "AzureDevOpsWorkItem", id, v, null);
    }
}
