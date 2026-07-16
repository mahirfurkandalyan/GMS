using Gms.Api.Domain;

namespace Gms.Api.Services.Integrations.Providers;

/// <summary>Result of validating an integration's static configuration (pre-activation).</summary>
public sealed class ProviderConfigValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
    public static ProviderConfigValidationResult Ok() => new();
    public ProviderConfigValidationResult Add(string error) { Errors.Add(error); return this; }
}

/// <summary>Result of a connection test.</summary>
public sealed record ProviderConnectionResult(bool Success, int? HttpStatusCode, string Message, int DurationMilliseconds);

/// <summary>
/// Everything a provider needs for an outgoing operation. Decrypted credentials are provided here
/// (built by the service immediately before the call) and MUST NOT be logged. The payload is the
/// GMS event/object data to deliver.
/// </summary>
public sealed class ProviderExecuteRequest
{
    public required IntegrationDefinition Definition { get; init; }
    public IntegrationEndpoint? Endpoint { get; init; }
    public required string Operation { get; init; }
    public required string CorrelationId { get; init; }
    public required IReadOnlyDictionary<string, string> DecryptedCredentials { get; init; }
    public object? Payload { get; init; }
}

/// <summary>Sanitized result of an outgoing operation (no secrets, length-limited summaries).</summary>
public sealed class ProviderExecuteResult
{
    public bool Success { get; init; }
    public int? HttpStatusCode { get; init; }
    public string? RequestSummary { get; init; }
    public string? ResponseSummary { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    /// <summary>Whether the failure is transient (eligible for retry). Ignored on success.</summary>
    public bool IsTransient { get; init; }

    public static ProviderExecuteResult Ok(int? status, string? reqSummary, string? respSummary) =>
        new() { Success = true, HttpStatusCode = status, RequestSummary = reqSummary, ResponseSummary = respSummary };

    public static ProviderExecuteResult Fail(int? status, string errorCode, string message, bool transient,
        string? reqSummary = null, string? respSummary = null) =>
        new()
        {
            Success = false, HttpStatusCode = status, ErrorCode = errorCode, ErrorMessage = message,
            IsTransient = transient, RequestSummary = reqSummary, ResponseSummary = respSummary
        };
}

/// <summary>Raw incoming webhook context handed to a provider for validation.</summary>
public sealed class IncomingWebhookContext
{
    public required IntegrationDefinition Definition { get; init; }
    public required string Body { get; init; }
    public required string? ContentType { get; init; }
    public required IReadOnlyDictionary<string, string> Headers { get; init; }
    /// <summary>Decrypted webhook secret (if configured); never logged.</summary>
    public string? WebhookSecret { get; init; }
}

/// <summary>Outcome of validating an incoming webhook.</summary>
public sealed class IncomingWebhookValidationResult
{
    public bool Accepted { get; init; }
    /// <summary>Suggested HTTP status: 202 accepted, 400 invalid, 401 bad signature, 409 duplicate.</summary>
    public int StatusCode { get; init; }
    public string? Reason { get; init; }
    /// <summary>Delivery id used for replay/duplicate detection (from payload or a header).</summary>
    public string? DeliveryId { get; init; }
    /// <summary>Allowlisted mapping this delivery corresponds to (null if none).</summary>
    public string? MappedAction { get; init; }
    /// <summary>Normalized external reference extracted from the payload (optional).</summary>
    public ExternalReference? Reference { get; init; }

    public static IncomingWebhookValidationResult Accept(string? deliveryId, string? action, ExternalReference? reference) =>
        new() { Accepted = true, StatusCode = 202, DeliveryId = deliveryId, MappedAction = action, Reference = reference };
    public static IncomingWebhookValidationResult Invalid(string reason) =>
        new() { Accepted = false, StatusCode = 400, Reason = reason };
    public static IncomingWebhookValidationResult BadSignature(string reason) =>
        new() { Accepted = false, StatusCode = 401, Reason = reason };
    public static IncomingWebhookValidationResult Duplicate(string deliveryId) =>
        new() { Accepted = false, StatusCode = 409, Reason = "Yinelenen teslimat.", DeliveryId = deliveryId };
}

/// <summary>Provider-normalized external object reference.</summary>
public sealed record ExternalReference(string ExternalObjectType, string ExternalObjectId, string? ExternalObjectKey, string? ExternalUrl);
