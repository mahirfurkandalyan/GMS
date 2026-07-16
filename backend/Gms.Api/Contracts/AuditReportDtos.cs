namespace Gms.Api.Contracts;

/* ── Shared metric DTOs ── */

/// <summary>A grouped metric bucket (e.g. status → count).</summary>
public class MetricBucketDto
{
    public string Key { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>A date-based time-series point (day-level grouping initially).</summary>
public class TimeSeriesPointDto
{
    public string Period { get; set; } = string.Empty; // yyyy-MM-dd
    public int Count { get; set; }
}

public class ActorActivityDto
{
    public Guid? ActorUserId { get; set; }
    public string ActorFullName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RecentItemDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Extra { get; set; }
    public DateTime CreatedAt { get; set; }
}

/* ── Unified audit ── */

public class UnifiedAuditRecordDto
{
    public Guid RecordId { get; set; }
    public string SourceModule { get; set; } = string.Empty;
    public string SourceTable { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public string? ActorFullName { get; set; }
    public string? ActorEmail { get; set; }
    public string ObjectType { get; set; } = string.Empty;
    public Guid? ObjectId { get; set; }
    public string? ObjectNumber { get; set; }
    public Guid? RelatedProjectId { get; set; }
    public Guid? RelatedEnvironmentId { get; set; }
    public string? Result { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditSummaryDto
{
    public int TotalEvents { get; set; }
    public int EventsToday { get; set; }
    public int EventsLast7Days { get; set; }
    public int FailedEvents { get; set; }
    public int SecurityEvents { get; set; }
    public List<ActorActivityDto> MostActiveUsers { get; set; } = new();
    public List<MetricBucketDto> EventsByModule { get; set; } = new();
    public List<MetricBucketDto> EventsByType { get; set; } = new();
}

/* ── Reports ── */

public class ReportOverviewDto
{
    public int TotalChanges { get; set; }
    public int OpenChanges { get; set; }
    public int ApprovedChanges { get; set; }
    public int ImplementedChanges { get; set; }
    public int CancelledChanges { get; set; }
    public int PendingApprovals { get; set; }
    public int ApprovedApprovals { get; set; }
    public int RejectedApprovals { get; set; }
    public int PlannedReleases { get; set; }
    public int ScheduledReleases { get; set; }
    public int CompletedReleases { get; set; }
    public int AcceptedReleases { get; set; }
    public int RunningExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public int RolledBackExecutions { get; set; }
    public int PassedValidations { get; set; }
    public int FailedValidations { get; set; }
    public int TotalDocuments { get; set; }
    public int UnreadNotifications { get; set; }
}

public class ChangeReportDto
{
    public List<MetricBucketDto> ChangesByStatus { get; set; } = new();
    public List<MetricBucketDto> ChangesByRisk { get; set; } = new();
    public List<MetricBucketDto> ChangesByClass { get; set; } = new();
    public List<MetricBucketDto> ChangesByType { get; set; } = new();
    public List<MetricBucketDto> ChangesByEnvironment { get; set; } = new();
    public double AverageHoursDraftToApproved { get; set; }
    public double AverageHoursApprovedToImplemented { get; set; }
    public List<RecentItemDto> HighRiskOpenChanges { get; set; } = new();
    public List<RecentItemDto> RecentChanges { get; set; } = new();
}

public class ApprovalReportDto
{
    public List<MetricBucketDto> ApprovalsByStatus { get; set; } = new();
    public double AverageApprovalDurationHours { get; set; }
    public List<MetricBucketDto> PendingByRole { get; set; } = new();
    public List<MetricBucketDto> RejectedByRole { get; set; } = new();
    public int OverdueSteps { get; set; }
    public List<ActorActivityDto> TopApprovers { get; set; } = new();
    public List<TimeSeriesPointDto> ApprovalVolumeByDate { get; set; } = new();
}

public class ReleaseReportDto
{
    public List<MetricBucketDto> ReleasesByStatus { get; set; } = new();
    public List<MetricBucketDto> ReleasesByType { get; set; } = new();
    public List<MetricBucketDto> ReleasesByEnvironment { get; set; } = new();
    public double AverageChangesPerRelease { get; set; }
    public double AverageReleaseRiskScore { get; set; }
    public List<RecentItemDto> UpcomingReleases { get; set; } = new();
    public int CompletedReleases { get; set; }
    public int CancelledReleases { get; set; }
    public List<TimeSeriesPointDto> ReleaseVolumeByDate { get; set; } = new();
}

public class ExecutionReportDto
{
    public List<MetricBucketDto> ExecutionsByStatus { get; set; } = new();
    public double SuccessRate { get; set; }
    public double FailureRate { get; set; }
    public double RollbackRate { get; set; }
    public double AverageExecutionDurationMinutes { get; set; }
    public int FailedSteps { get; set; }
    public List<RecentItemDto> RecentFailedExecutions { get; set; } = new();
    public List<TimeSeriesPointDto> ExecutionVolumeByDate { get; set; } = new();
}

public class ValidationReportDto
{
    public List<MetricBucketDto> ValidationsByStatus { get; set; } = new();
    public double PassRate { get; set; }
    public double FailRate { get; set; }
    public double AverageValidationDurationMinutes { get; set; }
    public int FailedChecks { get; set; }
    public List<MetricBucketDto> ValidationsByType { get; set; } = new();
    public List<TimeSeriesPointDto> ValidationVolumeByDate { get; set; } = new();
}

public class DocumentReportDto
{
    public List<MetricBucketDto> DocumentsByCategory { get; set; } = new();
    public List<MetricBucketDto> DocumentsByStatus { get; set; } = new();
    public int VersionsCreated { get; set; }
    public List<TimeSeriesPointDto> DownloadsByDate { get; set; } = new();
    public List<RecentItemDto> MostDownloadedDocuments { get; set; } = new();
    public int IntegrityFailures { get; set; }
    public long StorageBytesTotal { get; set; }
}

public class SecurityReportDto
{
    public int SuccessfulLogins { get; set; }
    public int FailedLogins { get; set; }
    public int Lockouts { get; set; }
    public int PasswordChanges { get; set; }
    public int TokenRefreshes { get; set; }
    public int Logouts { get; set; }
    public List<TimeSeriesPointDto> SecurityEventsByDate { get; set; } = new();
    public List<MetricBucketDto> TopFailedLoginEmails { get; set; } = new();
    public List<MetricBucketDto> TopSourceIpAddresses { get; set; } = new();
}

public class ReportCatalogItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequiredPermission { get; set; } = string.Empty;
    public List<string> SupportedFilters { get; set; } = new();
    public List<string> SupportedExportFormats { get; set; } = new();
}
