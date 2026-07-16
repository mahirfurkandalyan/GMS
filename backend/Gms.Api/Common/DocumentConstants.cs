namespace Gms.Api.Common;

/// <summary>
/// Document lifecycle statuses. Draft → Active (first file uploaded) → Archived;
/// any non-deleted state may be soft-Deleted; Deleted is terminal and cannot return.
/// Transitions owned by <see cref="Gms.Api.Domain.Document"/>.
/// </summary>
public static class DocumentStatuses
{
    public const string Draft = "Draft";
    public const string Active = "Active";
    public const string Archived = "Archived";
    public const string Deleted = "Deleted";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Draft, Active, Archived, Deleted
    };

    public static readonly IReadOnlyDictionary<string, HashSet<string>> Transitions = new Dictionary<string, HashSet<string>>
    {
        [Draft] = new() { Active, Deleted },
        [Active] = new() { Archived, Deleted },
        [Archived] = new() { Active, Deleted },
        [Deleted] = new()
    };
}

/// <summary>Seeded document category vocabulary (validated on create/update).</summary>
public static class DocumentCategories
{
    public const string SqlScript = "SQL Script";
    public const string RollbackScript = "Rollback Script";
    public const string CabMinutes = "CAB Minutes";
    public const string ValidationEvidence = "Validation Evidence";
    public const string ReleaseNote = "Release Note";
    public const string DeploymentGuide = "Deployment Guide";
    public const string RiskAnalysis = "Risk Analysis";
    public const string Sop = "SOP";
    public const string Checklist = "Checklist";
    public const string Configuration = "Configuration";
    public const string Certificate = "Certificate";
    public const string Log = "Log";
    public const string Screenshot = "Screenshot";
    public const string TestEvidence = "Test Evidence";
    public const string Other = "Other";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        SqlScript, RollbackScript, CabMinutes, ValidationEvidence, ReleaseNote, DeploymentGuide,
        RiskAnalysis, Sop, Checklist, Configuration, Certificate, Log, Screenshot, TestEvidence, Other
    };
}

/// <summary>Object types a document may be linked to (extensible for future domains).</summary>
public static class DocumentObjectTypes
{
    public const string Change = "Change";
    public const string Approval = "Approval";
    public const string Release = "Release";
    public const string Deployment = "Deployment";
    public const string Validation = "Validation";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        Change, Approval, Release, Deployment, Validation
    };
}

/// <summary>DocumentAuditEvent type vocabulary.</summary>
public static class DocumentEventTypes
{
    public const string DocumentCreated = "DocumentCreated";
    public const string DocumentUpdated = "DocumentUpdated";
    public const string Uploaded = "Uploaded";
    public const string VersionCreated = "VersionCreated";
    public const string Downloaded = "Downloaded";
    public const string Archived = "Archived";
    public const string Activated = "Activated";
    public const string Deleted = "Deleted";
    public const string Attached = "Attached";
    public const string Detached = "Detached";
    public const string IntegrityCheckFailed = "IntegrityCheckFailed";
}

/// <summary>File-upload validation rules. Dangerous/executable extensions are rejected.</summary>
public static class DocumentFileRules
{
    public const string HashAlgorithm = "SHA256";
    public const long MaxSizeBytes = 50L * 1024 * 1024; // 50 MB

    public static readonly IReadOnlySet<string> DangerousExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".bat", ".cmd", ".ps1", ".js", ".sh", ".msi", ".com", ".scr", ".vbs", ".jar", ".dll", ".app"
    };
}
