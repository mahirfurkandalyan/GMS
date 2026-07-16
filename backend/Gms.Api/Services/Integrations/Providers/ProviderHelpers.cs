using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gms.Api.Common;
using Gms.Api.Domain;

namespace Gms.Api.Services.Integrations.Providers;

/// <summary>Shared, secret-safe helpers used by provider adapters.</summary>
public static class ProviderHelpers
{
    public const string SignatureHeader = "X-Gms-Signature";
    public const string SecretHeader = "X-Webhook-Secret";
    public const string DeliveryIdHeader = "X-Delivery-Id";

    /// <summary>Computes a lowercase hex HMAC-SHA256 of the body using the secret.</summary>
    public static string ComputeSignature(string secret, string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>Constant-time comparison to avoid timing side channels on signatures.</summary>
    public static bool FixedEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a ?? string.Empty);
        var bb = Encoding.UTF8.GetBytes(b ?? string.Empty);
        return CryptographicOperations.FixedTimeEquals(
            ba.Length == bb.Length ? ba : new byte[bb.Length], bb) && ba.Length == bb.Length;
    }

    /// <summary>
    /// Validates a webhook secret: accepts either an HMAC signature over the body (preferred) or a
    /// direct shared-secret header. Returns null when valid, otherwise a reason string.
    /// </summary>
    public static string? ValidateWebhookSecret(IncomingWebhookContext ctx)
    {
        if (string.IsNullOrEmpty(ctx.WebhookSecret))
            return "Webhook gizli anahtarı yapılandırılmamış.";

        if (ctx.Headers.TryGetValue(SignatureHeader, out var sig) && !string.IsNullOrWhiteSpace(sig))
        {
            var expected = ComputeSignature(ctx.WebhookSecret, ctx.Body);
            return FixedEquals(sig.Trim(), expected) ? null : "İmza doğrulanamadı.";
        }
        if (ctx.Headers.TryGetValue(SecretHeader, out var secret) && !string.IsNullOrWhiteSpace(secret))
            return FixedEquals(secret.Trim(), ctx.WebhookSecret) ? null : "Gizli anahtar eşleşmedi.";

        return "İmza/gizli anahtar başlığı eksik.";
    }

    /// <summary>Reads the delivery id from a header or a top-level payload field (for dedup).</summary>
    public static string? ExtractDeliveryId(IncomingWebhookContext ctx)
    {
        if (ctx.Headers.TryGetValue(DeliveryIdHeader, out var h) && !string.IsNullOrWhiteSpace(h))
            return h.Trim();
        var el = TryParse(ctx.Body);
        if (el is { } root)
        {
            foreach (var name in new[] { "deliveryId", "id", "eventId" })
                if (root.TryGetProperty(name, out var v) && v.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                    return v.ToString();
        }
        return null;
    }

    public static JsonElement? TryParse(string body)
    {
        try { return JsonDocument.Parse(body).RootElement.Clone(); }
        catch { return null; }
    }

    public static string? GetString(JsonElement el, string name)
        => el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var v) ? v.ToString() : null;

    /// <summary>Builds auth headers from decrypted credentials (values never logged by callers).</summary>
    public static Dictionary<string, string> BuildAuthHeaders(IntegrationDefinition def, IReadOnlyDictionary<string, string> creds)
    {
        var headers = new Dictionary<string, string>();
        switch (def.AuthenticationType)
        {
            case IntegrationAuthTypes.ApiKey when creds.TryGetValue(IntegrationCredentialKeys.ApiKey, out var apiKey):
                headers["X-Api-Key"] = apiKey;
                break;
            case IntegrationAuthTypes.BearerToken when creds.TryGetValue(IntegrationCredentialKeys.BearerToken, out var token):
                headers["Authorization"] = $"Bearer {token}";
                break;
            case IntegrationAuthTypes.Basic
                when creds.TryGetValue(IntegrationCredentialKeys.BasicUsername, out var user)
                     && creds.TryGetValue(IntegrationCredentialKeys.BasicPassword, out var pass):
                var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}"));
                headers["Authorization"] = $"Basic {basic}";
                break;
        }
        return headers;
    }

    /// <summary>Serializes a payload to JSON for delivery (safe: payloads carry no secrets).</summary>
    public static string SerializePayload(object? payload)
        => payload is null ? "{}" : JsonSerializer.Serialize(payload);

    /// <summary>Combines base url + relative path safely.</summary>
    public static string CombineUrl(string? baseUrl, string? relativePath)
    {
        var b = (baseUrl ?? string.Empty).TrimEnd('/');
        var r = (relativePath ?? string.Empty).TrimStart('/');
        return string.IsNullOrEmpty(r) ? b : $"{b}/{r}";
    }
}
