namespace Gms.Api.Common;

/// <summary>Supported integration providers (adapter is resolved by this value).</summary>
public static class IntegrationProviders
{
    public const string GenericRest = "GenericRest";
    public const string IncomingWebhook = "IncomingWebhook";
    public const string OutgoingWebhook = "OutgoingWebhook";
    public const string Smtp = "Smtp";
    public const string AzureDevOps = "AzureDevOps";
    public const string Jira = "Jira";
    public const string GitHub = "GitHub";
    public const string GitLab = "GitLab";
    public const string Jenkins = "Jenkins";
    public const string ServiceNow = "ServiceNow";
    public const string Sap = "Sap";
    public const string Teams = "Teams";
    public const string Slack = "Slack";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        GenericRest, IncomingWebhook, OutgoingWebhook, Smtp, AzureDevOps, Jira,
        GitHub, GitLab, Jenkins, ServiceNow, Sap, Teams, Slack
    };

    /// <summary>Providers that have a concrete adapter implemented in this sprint.</summary>
    public static readonly IReadOnlySet<string> Implemented = new HashSet<string>
    {
        GenericRest, IncomingWebhook, OutgoingWebhook, Smtp, AzureDevOps, Jira
    };
}

/// <summary>Integration functional categories.</summary>
public static class IntegrationCategories
{
    public const string WorkManagement = "WorkManagement";
    public const string SourceControl = "SourceControl";
    public const string CiCd = "CiCd";
    public const string Communication = "Communication";
    public const string Email = "Email";
    public const string Enterprise = "Enterprise";
    public const string Generic = "Generic";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        WorkManagement, SourceControl, CiCd, Communication, Email, Enterprise, Generic
    };
}

/// <summary>IntegrationDefinition lifecycle statuses. Archived is terminal.</summary>
public static class IntegrationStatuses
{
    public const string Draft = "Draft";
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Failed = "Failed";
    public const string Archived = "Archived";

    public static readonly IReadOnlyDictionary<string, HashSet<string>> Transitions = new Dictionary<string, HashSet<string>>
    {
        [Draft] = new() { Active, Failed, Archived },
        [Active] = new() { Inactive, Failed, Archived },
        [Inactive] = new() { Active, Archived },
        [Failed] = new() { Active, Inactive, Archived },
        [Archived] = new()
    };
}

/// <summary>Authentication types an integration may use.</summary>
public static class IntegrationAuthTypes
{
    public const string None = "None";
    public const string ApiKey = "ApiKey";
    public const string BearerToken = "BearerToken";
    public const string Basic = "Basic";
    public const string OAuth2ClientCredentials = "OAuth2ClientCredentials";
    public const string WebhookSecret = "WebhookSecret";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        None, ApiKey, BearerToken, Basic, OAuth2ClientCredentials, WebhookSecret
    };
}

/// <summary>Credential key-name vocabulary (the KeyName column value; not the secret).</summary>
public static class IntegrationCredentialKeys
{
    public const string ApiKey = "ApiKey";
    public const string BearerToken = "BearerToken";
    public const string BasicUsername = "BasicUsername";
    public const string BasicPassword = "BasicPassword";
    public const string ClientId = "ClientId";
    public const string ClientSecret = "ClientSecret";
    public const string WebhookSecret = "WebhookSecret";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        ApiKey, BearerToken, BasicUsername, BasicPassword, ClientId, ClientSecret, WebhookSecret
    };

    /// <summary>Keys that hold a non-secret identifier (username / client id) — still masked in DTOs.</summary>
    public static readonly IReadOnlySet<string> NonSecret = new HashSet<string> { BasicUsername, ClientId };
}

/// <summary>Endpoint / execution direction.</summary>
public static class IntegrationDirections
{
    public const string Incoming = "Incoming";
    public const string Outgoing = "Outgoing";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Incoming, Outgoing };
}

/// <summary>Allowed HTTP methods for endpoints/outgoing requests.</summary>
public static class IntegrationHttpMethods
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "POST", "PUT", "PATCH", "DELETE"
    };
}

/// <summary>IntegrationExecution lifecycle statuses.</summary>
public static class IntegrationExecutionStatuses
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
    public const string DeadLetter = "DeadLetter";

    public static readonly IReadOnlyDictionary<string, HashSet<string>> Transitions = new Dictionary<string, HashSet<string>>
    {
        [Pending] = new() { Running, Cancelled },
        [Running] = new() { Succeeded, Failed, Cancelled },
        [Failed] = new() { Running, DeadLetter, Cancelled }, // Failed can be retried (→Running) or dead-lettered
        [Succeeded] = new(),
        [Cancelled] = new(),
        [DeadLetter] = new()
    };

    /// <summary>Statuses the dispatcher may pick up for (re)processing.</summary>
    public static readonly IReadOnlySet<string> Dispatchable = new HashSet<string> { Pending, Failed };
    public static readonly IReadOnlySet<string> Terminal = new HashSet<string> { Succeeded, Cancelled, DeadLetter };
}

/// <summary>IntegrationEvent audit type vocabulary.</summary>
public static class IntegrationEventTypes
{
    public const string IntegrationCreated = "IntegrationCreated";
    public const string IntegrationActivated = "IntegrationActivated";
    public const string IntegrationDeactivated = "IntegrationDeactivated";
    public const string CredentialAdded = "CredentialAdded";
    public const string CredentialRotated = "CredentialRotated";
    public const string ConnectionTestStarted = "ConnectionTestStarted";
    public const string ConnectionTestSucceeded = "ConnectionTestSucceeded";
    public const string ConnectionTestFailed = "ConnectionTestFailed";
    public const string IncomingWebhookAccepted = "IncomingWebhookAccepted";
    public const string IncomingWebhookRejected = "IncomingWebhookRejected";
    public const string OutgoingDeliveryStarted = "OutgoingDeliveryStarted";
    public const string OutgoingDeliverySucceeded = "OutgoingDeliverySucceeded";
    public const string OutgoingDeliveryFailed = "OutgoingDeliveryFailed";
    public const string RetryScheduled = "RetryScheduled";
    public const string DeadLettered = "DeadLettered";
    public const string ExternalObjectLinked = "ExternalObjectLinked";
    public const string ExternalObjectUnlinked = "ExternalObjectUnlinked";
}

/// <summary>GMS domain event types that outgoing subscriptions may deliver (this sprint's set).</summary>
public static class IntegrationSubscriptionEvents
{
    public const string ChangeSubmitted = "ChangeSubmitted";
    public const string WorkflowCompleted = "WorkflowCompleted";
    public const string ReleaseScheduled = "ReleaseScheduled";
    public const string ExecutionFailed = "ExecutionFailed";
    public const string ValidationFailed = "ValidationFailed";
    public const string DocumentUploaded = "DocumentUploaded";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        ChangeSubmitted, WorkflowCompleted, ReleaseScheduled, ExecutionFailed, ValidationFailed, DocumentUploaded
    };
}

/// <summary>
/// Allowlisted incoming-webhook → action mappings. An untrusted webhook payload can ONLY do
/// what a mapping here permits (never start arbitrary workflows). Automatic workflow triggering
/// is additionally gated by a configuration flag (disabled by default).
/// </summary>
public static class IntegrationWebhookMappings
{
    public const string JiraIssueReadyForReview = "JiraIssueReadyForReview";
    public const string AzureDevOpsWorkItemReadyForRelease = "AzureDevOpsWorkItemReadyForRelease";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        JiraIssueReadyForReview, AzureDevOpsWorkItemReadyForRelease
    };
}

/// <summary>Transient HTTP status codes that MAY be retried for outgoing requests.</summary>
public static class IntegrationRetry
{
    public const int MaxAttempts = 3;

    public static readonly IReadOnlySet<int> TransientStatusCodes = new HashSet<int> { 408, 429, 500, 502, 503, 504 };
}
