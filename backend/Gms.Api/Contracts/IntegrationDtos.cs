namespace Gms.Api.Contracts;

/* ── definition ─────────────────────────────────────────── */

public class IntegrationListDto
{
    public Guid Id { get; set; }
    public string IntegrationNo { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AuthenticationType { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSuccessfulConnectionAt { get; set; }
    public DateTime? LastFailedConnectionAt { get; set; }
}

public class IntegrationDetailDto
{
    public Guid Id { get; set; }
    public string IntegrationNo { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public string AuthenticationType { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastSuccessfulConnectionAt { get; set; }
    public DateTime? LastFailedConnectionAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public List<IntegrationCredentialDto> Credentials { get; set; } = new();
    public List<IntegrationEndpointDto> Endpoints { get; set; } = new();
    public List<IntegrationSubscriptionDto> Subscriptions { get; set; } = new();
}

/// <summary>Credential metadata only — the raw/encrypted secret is NEVER exposed.</summary>
public class IntegrationCredentialDto
{
    public Guid Id { get; set; }
    public string CredentialType { get; set; } = string.Empty;
    public string KeyName { get; set; } = string.Empty;
    public string MaskedValue { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? RotatedAt { get; set; }
}

public class IntegrationEndpointDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class IntegrationSubscriptionDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? ObjectType { get; set; }
    public Guid TargetEndpointId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class IntegrationProviderInfoDto
{
    public string Provider { get; set; } = string.Empty;
    public bool Implemented { get; set; }
    public bool SupportsIncoming { get; set; }
    public bool SupportsOutgoing { get; set; }
}

/* ── write DTOs ─────────────────────────────────────────── */

public class CreateIntegrationDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? BaseUrl { get; set; }
    public string? AuthenticationType { get; set; }
}

public class UpdateIntegrationDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? BaseUrl { get; set; }
    public string? AuthenticationType { get; set; }
    public string? RowVersion { get; set; }
}

/// <summary>Raw secret input. The raw value is protected immediately and never persisted/returned.</summary>
public class CreateCredentialDto
{
    public string KeyName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? CredentialType { get; set; }
}

public class UpdateCredentialDto
{
    public string Value { get; set; } = string.Empty;
}

public class CreateEndpointDto
{
    public string Name { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string? RelativePath { get; set; }
    public string? HttpMethod { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public bool IsActive { get; set; } = true;
}

public class UpdateEndpointDto
{
    public string? Name { get; set; }
    public string? RelativePath { get; set; }
    public string? HttpMethod { get; set; }
    public int? TimeoutSeconds { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateSubscriptionDto
{
    public string EventType { get; set; } = string.Empty;
    public string? ObjectType { get; set; }
    public Guid TargetEndpointId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateSubscriptionDto
{
    public string? EventType { get; set; }
    public string? ObjectType { get; set; }
    public Guid? TargetEndpointId { get; set; }
    public bool? IsActive { get; set; }
}

public class ConnectionTestResultDto
{
    public bool Success { get; set; }
    public int? HttpStatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public int DurationMilliseconds { get; set; }
}

/* ── executions ─────────────────────────────────────────── */

public class IntegrationExecutionListDto
{
    public Guid Id { get; set; }
    public string ExecutionNo { get; set; } = string.Empty;
    public Guid IntegrationDefinitionId { get; set; }
    public string IntegrationName { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? HttpStatusCode { get; set; }
    public int RetryCount { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class IntegrationExecutionDetailDto
{
    public Guid Id { get; set; }
    public string ExecutionNo { get; set; } = string.Empty;
    public Guid IntegrationDefinitionId { get; set; }
    public string IntegrationName { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string? ObjectType { get; set; }
    public Guid? ObjectId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? RequestSummary { get; set; }
    public string? ResponseSummary { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public List<IntegrationExecutionAttemptDto> Attempts { get; set; } = new();
    public List<IntegrationEventDto> Events { get; set; } = new();
}

public class IntegrationExecutionAttemptDto
{
    public Guid Id { get; set; }
    public int AttemptNo { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? HttpStatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int DurationMilliseconds { get; set; }
}

public class IntegrationEventDto
{
    public Guid Id { get; set; }
    public Guid? IntegrationExecutionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DispatchResultDto
{
    public int Processed { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public int DeadLettered { get; set; }
}

/* ── external links ─────────────────────────────────────── */

public class ExternalObjectLinkDto
{
    public Guid Id { get; set; }
    public Guid IntegrationDefinitionId { get; set; }
    public string IntegrationName { get; set; } = string.Empty;
    public string InternalObjectType { get; set; } = string.Empty;
    public Guid InternalObjectId { get; set; }
    public string ExternalObjectType { get; set; } = string.Empty;
    public string ExternalObjectId { get; set; } = string.Empty;
    public string? ExternalObjectKey { get; set; }
    public string? ExternalUrl { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }
}

public class CreateExternalLinkDto
{
    public string InternalObjectType { get; set; } = string.Empty;
    public Guid InternalObjectId { get; set; }
    public string? ExternalObjectType { get; set; }
    /// <summary>External key or id (provider-normalized). E.g. Jira issue key EBR-421.</summary>
    public string ExternalReference { get; set; } = string.Empty;
    public string? ExternalUrl { get; set; }
}

/// <summary>Link a change request to an external object via a chosen integration (internal fields derived from route).</summary>
public class LinkChangeExternalDto
{
    public Guid IntegrationId { get; set; }
    public string? ExternalObjectType { get; set; }
    public string ExternalReference { get; set; } = string.Empty;
    public string? ExternalUrl { get; set; }
}

/* ── reporting ──────────────────────────────────────────── */

public class IntegrationReportDto
{
    public List<MetricBucketDto> IntegrationsByProvider { get; set; } = new();
    public List<MetricBucketDto> IntegrationsByStatus { get; set; } = new();
    public List<MetricBucketDto> ExecutionsByStatus { get; set; } = new();
    public double SuccessRate { get; set; }
    public double FailureRate { get; set; }
    public double RetryRate { get; set; }
    public int DeadLetterCount { get; set; }
    public double AverageExecutionDurationMilliseconds { get; set; }
    public List<RecentItemDto> RecentFailures { get; set; } = new();
    public List<TimeSeriesPointDto> ExecutionsByDate { get; set; } = new();
}
