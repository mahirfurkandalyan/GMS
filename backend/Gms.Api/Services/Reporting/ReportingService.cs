using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Services.Reporting;

/// <summary>Common report filter (applied in SQL where the entity carries the column).</summary>
public sealed class ReportFilter
{
    public Guid? CustomerId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? EnvironmentId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public interface IReportingService
{
    Task<ReportOverviewDto> OverviewAsync(ReportFilter f, CancellationToken ct = default);
    Task<ChangeReportDto> ChangesAsync(ReportFilter f, CancellationToken ct = default);
    Task<ApprovalReportDto> ApprovalsAsync(ReportFilter f, CancellationToken ct = default);
    Task<ReleaseReportDto> ReleasesAsync(ReportFilter f, CancellationToken ct = default);
    Task<ExecutionReportDto> ExecutionsAsync(ReportFilter f, CancellationToken ct = default);
    Task<ValidationReportDto> ValidationsAsync(ReportFilter f, CancellationToken ct = default);
    Task<DocumentReportDto> DocumentsAsync(ReportFilter f, CancellationToken ct = default);
    Task<SecurityReportDto> SecurityAsync(ReportFilter f, CancellationToken ct = default);
    Task<IntegrationReportDto> IntegrationsAsync(ReportFilter f, CancellationToken ct = default);
}

/// <summary>
/// Read-only reporting service. Every metric is computed against real SQL Server data via
/// grouped queries (AsNoTracking, projection, no full-aggregate loading, no N+1). No mock data.
/// </summary>
public sealed class ReportingService : IReportingService
{
    private readonly GmsDbContext _db;
    public ReportingService(GmsDbContext db) => _db = db;

    public async Task<ReportOverviewDto> OverviewAsync(ReportFilter f, CancellationToken ct = default)
    {
        var changes = FilterChanges(f);
        var releases = FilterReleases(f);
        var approvals = ByDate(_db.ApprovalRequests.AsNoTracking(), f, a => a.CreatedAt);
        var runs = ByDate(_db.DeploymentRuns.AsNoTracking(), f, d => d.CreatedAt);
        var validations = ByDate(_db.ValidationRuns.AsNoTracking(), f, v => v.CreatedAt);

        return new ReportOverviewDto
        {
            TotalChanges = await changes.CountAsync(ct),
            OpenChanges = await changes.CountAsync(c => c.Status == ChangeStatuses.Draft || c.Status == ChangeStatuses.Submitted || c.Status == ChangeStatuses.UnderReview, ct),
            ApprovedChanges = await changes.CountAsync(c => c.Status == ChangeStatuses.Approved, ct),
            ImplementedChanges = await changes.CountAsync(c => c.Status == ChangeStatuses.Implemented, ct),
            CancelledChanges = await changes.CountAsync(c => c.Status == ChangeStatuses.Cancelled, ct),
            PendingApprovals = await approvals.CountAsync(a => a.Status == ApprovalStatuses.InProgress, ct),
            ApprovedApprovals = await approvals.CountAsync(a => a.Status == ApprovalStatuses.Approved, ct),
            RejectedApprovals = await approvals.CountAsync(a => a.Status == ApprovalStatuses.Rejected, ct),
            PlannedReleases = await releases.CountAsync(r => r.Status == ReleaseStatuses.Planned, ct),
            ScheduledReleases = await releases.CountAsync(r => r.Status == ReleaseStatuses.Scheduled, ct),
            CompletedReleases = await releases.CountAsync(r => r.Status == ReleaseStatuses.Completed, ct),
            AcceptedReleases = await releases.CountAsync(r => r.Status == ReleaseStatuses.Accepted, ct),
            RunningExecutions = await runs.CountAsync(r => r.Status == DeploymentRunStatuses.Running, ct),
            FailedExecutions = await runs.CountAsync(r => r.Status == DeploymentRunStatuses.Failed, ct),
            RolledBackExecutions = await runs.CountAsync(r => r.Status == DeploymentRunStatuses.RolledBack, ct),
            PassedValidations = await validations.CountAsync(v => v.Status == ValidationRunStatuses.Passed, ct),
            FailedValidations = await validations.CountAsync(v => v.Status == ValidationRunStatuses.Failed, ct),
            TotalDocuments = await _db.Documents.AsNoTracking().CountAsync(d => d.Status != DocumentStatuses.Deleted, ct),
            UnreadNotifications = await _db.Notifications.AsNoTracking().CountAsync(n => n.Status == NotificationStatuses.Unread, ct)
        };
    }

    public async Task<ChangeReportDto> ChangesAsync(ReportFilter f, CancellationToken ct = default)
    {
        var q = FilterChanges(f);

        var approvedDurations = _db.ChangeAuditEvents.AsNoTracking()
            .Where(e => e.EventType == ChangeAuditEventTypes.ChangeApproved)
            .Join(q, e => e.ChangeRequestId, c => c.Id, (e, c) => EF.Functions.DateDiffMinute(c.CreatedAt, e.CreatedAt));

        var approvedTimes = _db.ChangeAuditEvents.AsNoTracking().Where(e => e.EventType == ChangeAuditEventTypes.ChangeApproved)
            .GroupBy(e => e.ChangeRequestId).Select(g => new { Id = g.Key, T = g.Max(x => x.CreatedAt) });
        var implementedTimes = _db.ChangeAuditEvents.AsNoTracking().Where(e => e.EventType == ChangeAuditEventTypes.ChangeImplemented)
            .GroupBy(e => e.ChangeRequestId).Select(g => new { Id = g.Key, T = g.Max(x => x.CreatedAt) });
        var implDurations = approvedTimes.Join(implementedTimes, a => a.Id, i => i.Id, (a, i) => EF.Functions.DateDiffMinute(a.T, i.T));

        return new ChangeReportDto
        {
            ChangesByStatus = await Buckets(q, c => c.Status, ct),
            ChangesByRisk = await Buckets(q, c => c.RiskLevel, ct),
            ChangesByClass = await Buckets(q, c => c.ChangeClass, ct),
            ChangesByType = await Buckets(q, c => c.ChangeType, ct),
            ChangesByEnvironment = await Buckets(q, c => c.Environment!.Name, ct),
            AverageHoursDraftToApproved = Math.Round(await AvgOrZero(approvedDurations, ct) / 60.0, 2),
            AverageHoursApprovedToImplemented = Math.Round(await AvgOrZero(implDurations, ct) / 60.0, 2),
            HighRiskOpenChanges = await q.Where(c => (c.RiskLevel == ChangeRiskLevels.High || c.RiskLevel == ChangeRiskLevels.Critical)
                    && (c.Status == ChangeStatuses.Draft || c.Status == ChangeStatuses.Submitted || c.Status == ChangeStatuses.UnderReview || c.Status == ChangeStatuses.Approved))
                .OrderByDescending(c => c.CreatedAt).Take(10)
                .Select(c => new RecentItemDto { Id = c.Id, Number = c.ChangeNo, Title = c.Title, Status = c.Status, Extra = c.RiskLevel, CreatedAt = c.CreatedAt }).ToListAsync(ct),
            RecentChanges = await q.OrderByDescending(c => c.CreatedAt).Take(10)
                .Select(c => new RecentItemDto { Id = c.Id, Number = c.ChangeNo, Title = c.Title, Status = c.Status, Extra = c.RiskLevel, CreatedAt = c.CreatedAt }).ToListAsync(ct)
        };
    }

    public async Task<ApprovalReportDto> ApprovalsAsync(ReportFilter f, CancellationToken ct = default)
    {
        var q = ByDate(_db.ApprovalRequests.AsNoTracking(), f, a => a.CreatedAt);

        var durations = q.Where(a => a.CompletedAt != null)
            .Select(a => EF.Functions.DateDiffMinute(a.CreatedAt, a.CompletedAt!.Value));

        var steps = _db.ApprovalSteps.AsNoTracking();
        var pendingByRole = await steps.Where(s => s.Status == ApprovalStepStatuses.Active)
            .GroupBy(s => s.ApproverRole).Select(g => new MetricBucketDto { Key = g.Key, Count = g.Count() }).ToListAsync(ct);
        var rejectedByRole = await steps.Where(s => s.Status == ApprovalStepStatuses.Rejected)
            .GroupBy(s => s.ApproverRole).Select(g => new MetricBucketDto { Key = g.Key, Count = g.Count() }).ToListAsync(ct);
        var overdue = await steps.CountAsync(s => s.Status == ApprovalStepStatuses.Active && s.DueDate != null && s.DueDate < DateTime.UtcNow, ct);

        var topApproversRaw = await _db.ApprovalDecisions.AsNoTracking()
            .Where(d => d.Decision == ApprovalDecisions.Approved)
            .GroupBy(d => d.SignedByUserId).Select(g => new { g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).Take(5).ToListAsync(ct);
        var ids = topApproversRaw.Select(x => x.Key).ToList();
        var names = await _db.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).Select(u => new { u.Id, u.FullName }).ToListAsync(ct);
        var topApprovers = topApproversRaw.Select(x => new ActorActivityDto { ActorUserId = x.Key, ActorFullName = names.FirstOrDefault(n => n.Id == x.Key)?.FullName ?? string.Empty, Count = x.Count }).ToList();

        return new ApprovalReportDto
        {
            ApprovalsByStatus = await Buckets(q, a => a.Status, ct),
            AverageApprovalDurationHours = Math.Round(await AvgOrZero(durations, ct) / 60.0, 2),
            PendingByRole = pendingByRole, RejectedByRole = rejectedByRole, OverdueSteps = overdue,
            TopApprovers = topApprovers,
            ApprovalVolumeByDate = await Volume(q, a => a.CreatedAt, ct)
        };
    }

    public async Task<ReleaseReportDto> ReleasesAsync(ReportFilter f, CancellationToken ct = default)
    {
        var q = FilterReleases(f);

        // Average changes-per-release: compute counts separately (AVG(subquery) is invalid in SQL).
        var releaseCount = await q.CountAsync(ct);
        var itemCount = await _db.ReleasePlanItems.AsNoTracking().CountAsync(i => q.Select(r => r.Id).Contains(i.ReleasePlanId), ct);
        var avgChanges = releaseCount == 0 ? 0 : Math.Round((double)itemCount / releaseCount, 2);

        return new ReleaseReportDto
        {
            ReleasesByStatus = await Buckets(q, r => r.Status, ct),
            ReleasesByType = await Buckets(q, r => r.ReleaseType, ct),
            ReleasesByEnvironment = await Buckets(q, r => r.Environment!.Name, ct),
            AverageChangesPerRelease = avgChanges,
            AverageReleaseRiskScore = Math.Round(await AvgOrZeroInt(q.Select(r => r.RiskScore), ct), 2),
            CompletedReleases = await q.CountAsync(r => r.Status == ReleaseStatuses.Completed || r.Status == ReleaseStatuses.Accepted, ct),
            CancelledReleases = await q.CountAsync(r => r.Status == ReleaseStatuses.Cancelled, ct),
            UpcomingReleases = await q.Where(r => r.Status == ReleaseStatuses.Scheduled && r.PlannedDeploymentStart != null)
                .OrderBy(r => r.PlannedDeploymentStart).Take(10)
                .Select(r => new RecentItemDto { Id = r.Id, Number = r.ReleaseNo, Title = r.Name, Status = r.Status, Extra = r.ReleaseType, CreatedAt = r.CreatedAt }).ToListAsync(ct),
            ReleaseVolumeByDate = await Volume(q, r => r.CreatedAt, ct)
        };
    }

    public async Task<ExecutionReportDto> ExecutionsAsync(ReportFilter f, CancellationToken ct = default)
    {
        var q = ByDate(_db.DeploymentRuns.AsNoTracking(), f, d => d.CreatedAt);
        var total = await q.CountAsync(ct);
        var completed = await q.CountAsync(r => r.Status == DeploymentRunStatuses.Completed, ct);
        var failed = await q.CountAsync(r => r.Status == DeploymentRunStatuses.Failed, ct);
        var rolledBack = await q.CountAsync(r => r.Status == DeploymentRunStatuses.RolledBack, ct);
        var durations = q.Where(r => r.StartedAt != null && r.CompletedAt != null)
            .Select(r => EF.Functions.DateDiffMinute(r.StartedAt!.Value, r.CompletedAt!.Value));

        return new ExecutionReportDto
        {
            ExecutionsByStatus = await Buckets(q, r => r.Status, ct),
            SuccessRate = Rate(completed, total),
            FailureRate = Rate(failed, total),
            RollbackRate = Rate(rolledBack, total),
            AverageExecutionDurationMinutes = Math.Round(await AvgOrZero(durations, ct), 2),
            FailedSteps = await _db.DeploymentSteps.AsNoTracking().CountAsync(s => s.Status == DeploymentStepStatuses.Failed, ct),
            RecentFailedExecutions = await q.Where(r => r.Status == DeploymentRunStatuses.Failed || r.Status == DeploymentRunStatuses.RolledBack)
                .OrderByDescending(r => r.CreatedAt).Take(10)
                .Select(r => new RecentItemDto { Id = r.Id, Number = r.ExecutionNo, Title = r.ReleasePlan!.Name, Status = r.Status, Extra = r.OverallResult, CreatedAt = r.CreatedAt }).ToListAsync(ct),
            ExecutionVolumeByDate = await Volume(q, r => r.CreatedAt, ct)
        };
    }

    public async Task<ValidationReportDto> ValidationsAsync(ReportFilter f, CancellationToken ct = default)
    {
        var q = ByDate(_db.ValidationRuns.AsNoTracking(), f, v => v.CreatedAt);
        var total = await q.CountAsync(ct);
        var passed = await q.CountAsync(v => v.Status == ValidationRunStatuses.Passed, ct);
        var failed = await q.CountAsync(v => v.Status == ValidationRunStatuses.Failed, ct);
        var durations = q.Where(v => v.StartedAt != null && v.CompletedAt != null)
            .Select(v => EF.Functions.DateDiffMinute(v.StartedAt!.Value, v.CompletedAt!.Value));

        return new ValidationReportDto
        {
            ValidationsByStatus = await Buckets(q, v => v.Status, ct),
            PassRate = Rate(passed, total),
            FailRate = Rate(failed, total),
            AverageValidationDurationMinutes = Math.Round(await AvgOrZero(durations, ct), 2),
            FailedChecks = await _db.ValidationChecks.AsNoTracking().CountAsync(c => c.Status == ValidationCheckStatuses.Failed, ct),
            ValidationsByType = await Buckets(q, v => v.ValidationType, ct),
            ValidationVolumeByDate = await Volume(q, v => v.CreatedAt, ct)
        };
    }

    public async Task<DocumentReportDto> DocumentsAsync(ReportFilter f, CancellationToken ct = default)
    {
        var docs = ByDate(_db.Documents.AsNoTracking(), f, d => d.CreatedAt);
        var downloads = ByDate(_db.DocumentDownloads.AsNoTracking(), f, d => d.DownloadedAt);

        var mostDownloadedRaw = await _db.DocumentDownloads.AsNoTracking()
            .GroupBy(d => d.DocumentId).Select(g => new { g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).Take(10).ToListAsync(ct);
        var docIds = mostDownloadedRaw.Select(x => x.Key).ToList();
        var docInfo = await _db.Documents.AsNoTracking().Where(d => docIds.Contains(d.Id))
            .Select(d => new { d.Id, d.DocumentNo, d.Title, d.Status }).ToListAsync(ct);
        var mostDownloaded = mostDownloadedRaw.Select(x =>
        {
            var d = docInfo.FirstOrDefault(i => i.Id == x.Key);
            return new RecentItemDto { Id = x.Key, Number = d?.DocumentNo ?? string.Empty, Title = d?.Title ?? string.Empty, Status = d?.Status ?? string.Empty, Extra = $"{x.Count} indirme", CreatedAt = DateTime.MinValue };
        }).ToList();

        return new DocumentReportDto
        {
            DocumentsByCategory = await Buckets(docs, d => d.Category, ct),
            DocumentsByStatus = await Buckets(docs, d => d.Status, ct),
            VersionsCreated = await _db.DocumentVersions.AsNoTracking().CountAsync(ct),
            DownloadsByDate = await Volume(downloads, d => d.DownloadedAt, ct),
            MostDownloadedDocuments = mostDownloaded,
            IntegrityFailures = await _db.DocumentAuditEvents.AsNoTracking().CountAsync(e => e.EventType == DocumentEventTypes.IntegrityCheckFailed, ct),
            StorageBytesTotal = await _db.DocumentVersions.AsNoTracking().SumAsync(v => (long?)v.SizeBytes, ct) ?? 0
        };
    }

    public async Task<SecurityReportDto> SecurityAsync(ReportFilter f, CancellationToken ct = default)
    {
        var q = ByDate(_db.SecurityAuditEvents.AsNoTracking(), f, e => e.CreatedAt);

        var topFailedEmails = await q.Where(e => e.EventType == SecurityEventTypes.LoginFailed && e.Email != "")
            .GroupBy(e => e.Email).Select(g => new MetricBucketDto { Key = g.Key, Count = g.Count() })
            .OrderByDescending(b => b.Count).Take(10).ToListAsync(ct);
        var topIps = await q.Where(e => e.IpAddress != null)
            .GroupBy(e => e.IpAddress!).Select(g => new MetricBucketDto { Key = g.Key, Count = g.Count() })
            .OrderByDescending(b => b.Count).Take(10).ToListAsync(ct);

        return new SecurityReportDto
        {
            SuccessfulLogins = await q.CountAsync(e => e.EventType == SecurityEventTypes.LoginSucceeded, ct),
            FailedLogins = await q.CountAsync(e => e.EventType == SecurityEventTypes.LoginFailed, ct),
            Lockouts = await q.CountAsync(e => e.EventType == SecurityEventTypes.UserLockedOut, ct),
            PasswordChanges = await q.CountAsync(e => e.EventType == SecurityEventTypes.PasswordChanged, ct),
            TokenRefreshes = await q.CountAsync(e => e.EventType == SecurityEventTypes.TokenRefreshed, ct),
            Logouts = await q.CountAsync(e => e.EventType == SecurityEventTypes.Logout || e.EventType == SecurityEventTypes.LogoutAll, ct),
            SecurityEventsByDate = await Volume(q, e => e.CreatedAt, ct),
            TopFailedLoginEmails = topFailedEmails,
            TopSourceIpAddresses = topIps
        };
    }

    public async Task<IntegrationReportDto> IntegrationsAsync(ReportFilter f, CancellationToken ct = default)
    {
        var defs = _db.IntegrationDefinitions.AsNoTracking();
        var execs = ByDate(_db.IntegrationExecutions.AsNoTracking(), f, x => x.CreatedAt);

        var total = await execs.CountAsync(ct);
        var succeeded = await execs.CountAsync(x => x.Status == IntegrationExecutionStatuses.Succeeded, ct);
        var failed = await execs.CountAsync(x => x.Status == IntegrationExecutionStatuses.Failed || x.Status == IntegrationExecutionStatuses.DeadLetter, ct);
        var retried = await execs.CountAsync(x => x.RetryCount > 0, ct);
        var deadLetter = await execs.CountAsync(x => x.Status == IntegrationExecutionStatuses.DeadLetter, ct);
        var durations = _db.IntegrationExecutionAttempts.AsNoTracking().Where(a => a.Status == "Succeeded")
            .Select(a => a.DurationMilliseconds);

        return new IntegrationReportDto
        {
            IntegrationsByProvider = await Buckets(defs, d => d.Provider, ct),
            IntegrationsByStatus = await Buckets(defs, d => d.Status, ct),
            ExecutionsByStatus = await Buckets(execs, x => x.Status, ct),
            SuccessRate = Rate(succeeded, total),
            FailureRate = Rate(failed, total),
            RetryRate = Rate(retried, total),
            DeadLetterCount = deadLetter,
            AverageExecutionDurationMilliseconds = Math.Round(await AvgOrZero(durations, ct), 2),
            RecentFailures = await execs.Where(x => x.Status == IntegrationExecutionStatuses.Failed || x.Status == IntegrationExecutionStatuses.DeadLetter)
                .OrderByDescending(x => x.CreatedAt).Take(10)
                .Select(x => new RecentItemDto { Id = x.Id, Number = x.ExecutionNo, Title = x.IntegrationDefinition!.Name, Status = x.Status, Extra = x.ErrorCode, CreatedAt = x.CreatedAt }).ToListAsync(ct),
            ExecutionsByDate = await Volume(execs, x => x.CreatedAt, ct)
        };
    }

    /* ── query helpers ── */

    private IQueryable<Domain.ChangeRequest> FilterChanges(ReportFilter f)
    {
        var q = _db.ChangeRequests.AsNoTracking().AsQueryable();
        if (f.CustomerId.HasValue) q = q.Where(c => c.CustomerId == f.CustomerId);
        if (f.ProjectId.HasValue) q = q.Where(c => c.ProjectId == f.ProjectId);
        if (f.EnvironmentId.HasValue) q = q.Where(c => c.EnvironmentId == f.EnvironmentId);
        if (f.DateFrom.HasValue) q = q.Where(c => c.CreatedAt >= f.DateFrom.Value);
        if (f.DateTo.HasValue) q = q.Where(c => c.CreatedAt <= f.DateTo.Value);
        return q;
    }

    private IQueryable<Domain.ReleasePlan> FilterReleases(ReportFilter f)
    {
        var q = _db.ReleasePlans.AsNoTracking().AsQueryable();
        if (f.CustomerId.HasValue) q = q.Where(r => r.CustomerId == f.CustomerId);
        if (f.ProjectId.HasValue) q = q.Where(r => r.ProjectId == f.ProjectId);
        if (f.EnvironmentId.HasValue) q = q.Where(r => r.EnvironmentId == f.EnvironmentId);
        if (f.DateFrom.HasValue) q = q.Where(r => r.CreatedAt >= f.DateFrom.Value);
        if (f.DateTo.HasValue) q = q.Where(r => r.CreatedAt <= f.DateTo.Value);
        return q;
    }

    private static IQueryable<T> ByDate<T>(IQueryable<T> q, ReportFilter f, System.Linq.Expressions.Expression<Func<T, DateTime>> date)
    {
        // Only the date range applies to entities without customer/project/environment columns.
        if (f.DateFrom.HasValue)
        {
            var from = f.DateFrom.Value;
            q = q.Where(BuildCompare(date, from, greaterOrEqual: true));
        }
        if (f.DateTo.HasValue)
        {
            var to = f.DateTo.Value;
            q = q.Where(BuildCompare(date, to, greaterOrEqual: false));
        }
        return q;
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> BuildCompare<T>(
        System.Linq.Expressions.Expression<Func<T, DateTime>> date, DateTime value, bool greaterOrEqual)
    {
        var p = date.Parameters[0];
        var body = greaterOrEqual
            ? System.Linq.Expressions.Expression.GreaterThanOrEqual(date.Body, System.Linq.Expressions.Expression.Constant(value))
            : System.Linq.Expressions.Expression.LessThanOrEqual(date.Body, System.Linq.Expressions.Expression.Constant(value));
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(body, p);
    }

    private static async Task<List<MetricBucketDto>> Buckets<T>(IQueryable<T> q, System.Linq.Expressions.Expression<Func<T, string>> key, CancellationToken ct) =>
        await q.GroupBy(key).Select(g => new MetricBucketDto { Key = g.Key, Count = g.Count() }).OrderByDescending(b => b.Count).ToListAsync(ct);

    private static async Task<List<TimeSeriesPointDto>> Volume<T>(IQueryable<T> q, System.Linq.Expressions.Expression<Func<T, DateTime>> date, CancellationToken ct)
    {
        var raw = await q.GroupBy(BuildDateKey(date)).Select(g => new { D = g.Key, C = g.Count() }).OrderBy(x => x.D).ToListAsync(ct);
        return raw.Select(x => new TimeSeriesPointDto { Period = x.D.ToString("yyyy-MM-dd"), Count = x.C }).ToList();
    }

    private static System.Linq.Expressions.Expression<Func<T, DateTime>> BuildDateKey<T>(System.Linq.Expressions.Expression<Func<T, DateTime>> date)
    {
        var dateProp = System.Linq.Expressions.Expression.Property(date.Body, nameof(DateTime.Date));
        return System.Linq.Expressions.Expression.Lambda<Func<T, DateTime>>(dateProp, date.Parameters);
    }

    private static async Task<double> AvgOrZero(IQueryable<int> q, CancellationToken ct) =>
        await q.AnyAsync(ct) ? await q.AverageAsync(ct) : 0;

    private static async Task<double> AvgOrZeroInt(IQueryable<int> q, CancellationToken ct) =>
        await q.AnyAsync(ct) ? await q.AverageAsync(ct) : 0;

    private static double Rate(int part, int total) => total == 0 ? 0 : Math.Round(part * 100.0 / total, 1);
}
