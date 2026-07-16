namespace Gms.Api.Common;

/// <summary>
/// Notification lifecycle statuses. Unread → Read → Archived; any state may be soft-Deleted
/// (optional). Transitions owned by <see cref="Gms.Api.Domain.Notification"/>.
/// </summary>
public static class NotificationStatuses
{
    public const string Unread = "Unread";
    public const string Read = "Read";
    public const string Archived = "Archived";
    public const string Deleted = "Deleted";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Unread, Read, Archived, Deleted };

    public static readonly IReadOnlyDictionary<string, HashSet<string>> Transitions = new Dictionary<string, HashSet<string>>
    {
        [Unread] = new() { Read, Archived, Deleted },
        [Read] = new() { Archived, Deleted },
        [Archived] = new() { Deleted },
        [Deleted] = new()
    };
}

/// <summary>Notification severity vocabulary.</summary>
public static class NotificationSeverities
{
    public const string Information = "Information";
    public const string Success = "Success";
    public const string Warning = "Warning";
    public const string Error = "Error";
    public const string Critical = "Critical";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Information, Success, Warning, Error, Critical
    };
}

/// <summary>Delivery channels (designed for extension: SMS/Teams/Slack/WebHook later).</summary>
public static class NotificationChannels
{
    public const string InApp = "InApp";
    public const string Email = "Email";
}

/// <summary>Delivery status vocabulary.</summary>
public static class NotificationDeliveryStatuses
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Sent = "Sent";
    public const string Delivered = "Delivered";
    public const string Failed = "Failed";
    public const string DeadLetter = "DeadLetter";
}

/// <summary>Modules a notification/preference belongs to (used for per-module preferences).</summary>
public static class NotificationModules
{
    public const string Change = "CHANGE";
    public const string Approval = "APPROVAL";
    public const string Release = "RELEASE";
    public const string Execution = "EXECUTION";
    public const string Validation = "VALIDATION";
    public const string Document = "DOCUMENT";
    public const string Security = "SECURITY";
    public const string System = "SYSTEM";
    public const string Workflow = "WORKFLOW";
    public const string Integration = "INTEGRATION";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Change, Approval, Release, Execution, Validation, Document, Security, System, Workflow, Integration
    };
}

/// <summary>NotificationEvent audit type vocabulary.</summary>
public static class NotificationEventTypes
{
    public const string Created = "Created";
    public const string InAppDelivered = "InAppDelivered";
    public const string EmailQueued = "EmailQueued";
    public const string EmailSent = "EmailSent";
    public const string EmailFailed = "EmailFailed";
    public const string Read = "Read";
    public const string Archived = "Archived";
    public const string Deleted = "Deleted";
    public const string Broadcast = "Broadcast";
}
