using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Gms.Api.Services.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gms.Api.Controllers;

/// <summary>
/// Read-only reporting endpoints. Every metric is computed from real SQL Server data in
/// <see cref="IReportingService"/> (grouped queries). Thin controller. No mock statistics.
/// </summary>
[ApiController]
[Route("api/reports")]
[Tags("Reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reports;
    private readonly IReportExportService _export;
    private readonly GmsDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ReportsController(IReportingService reports, IReportExportService export, GmsDbContext db, ICurrentUser currentUser)
    {
        _reports = reports;
        _export = export;
        _db = db;
        _currentUser = currentUser;
    }

    private static ReportFilter Filter(Guid? customerId, Guid? projectId, Guid? environmentId, DateTime? dateFrom, DateTime? dateTo) =>
        new() { CustomerId = customerId, ProjectId = projectId, EnvironmentId = environmentId, DateFrom = dateFrom, DateTo = dateTo };

    [HttpGet("overview")]
    [Authorize(Policy = Permissions.ReportRead)]
    public async Task<ActionResult<ReportOverviewDto>> Overview([FromQuery] Guid? customerId, [FromQuery] Guid? projectId, [FromQuery] Guid? environmentId, [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, CancellationToken ct)
        => Ok(await _reports.OverviewAsync(Filter(customerId, projectId, environmentId, dateFrom, dateTo), ct));

    [HttpGet("changes")]
    [Authorize(Policy = Permissions.ReportRead)]
    public async Task<ActionResult<ChangeReportDto>> Changes([FromQuery] Guid? customerId, [FromQuery] Guid? projectId, [FromQuery] Guid? environmentId, [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, CancellationToken ct)
        => Ok(await _reports.ChangesAsync(Filter(customerId, projectId, environmentId, dateFrom, dateTo), ct));

    [HttpGet("approvals")]
    [Authorize(Policy = Permissions.ReportRead)]
    public async Task<ActionResult<ApprovalReportDto>> Approvals([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, CancellationToken ct)
        => Ok(await _reports.ApprovalsAsync(Filter(null, null, null, dateFrom, dateTo), ct));

    [HttpGet("releases")]
    [Authorize(Policy = Permissions.ReportRead)]
    public async Task<ActionResult<ReleaseReportDto>> Releases([FromQuery] Guid? customerId, [FromQuery] Guid? projectId, [FromQuery] Guid? environmentId, [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, CancellationToken ct)
        => Ok(await _reports.ReleasesAsync(Filter(customerId, projectId, environmentId, dateFrom, dateTo), ct));

    [HttpGet("executions")]
    [Authorize(Policy = Permissions.ReportRead)]
    public async Task<ActionResult<ExecutionReportDto>> Executions([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, CancellationToken ct)
        => Ok(await _reports.ExecutionsAsync(Filter(null, null, null, dateFrom, dateTo), ct));

    [HttpGet("validations")]
    [Authorize(Policy = Permissions.ReportRead)]
    public async Task<ActionResult<ValidationReportDto>> Validations([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, CancellationToken ct)
        => Ok(await _reports.ValidationsAsync(Filter(null, null, null, dateFrom, dateTo), ct));

    [HttpGet("documents")]
    [Authorize(Policy = Permissions.ReportRead)]
    public async Task<ActionResult<DocumentReportDto>> Documents([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, CancellationToken ct)
        => Ok(await _reports.DocumentsAsync(Filter(null, null, null, dateFrom, dateTo), ct));

    /// <summary>Güvenlik raporu — yalnızca audit.security.read (Admin/Auditor).</summary>
    [HttpGet("security")]
    [Authorize(Policy = Permissions.AuditSecurityRead)]
    public async Task<ActionResult<SecurityReportDto>> Security([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, CancellationToken ct)
        => Ok(await _reports.SecurityAsync(Filter(null, null, null, dateFrom, dateTo), ct));

    /// <summary>Entegrasyon (Integration Hub) raporu — gerçek metrikler.</summary>
    [HttpGet("integrations")]
    [Authorize(Policy = Permissions.ReportRead)]
    public async Task<ActionResult<IntegrationReportDto>> Integrations([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, CancellationToken ct)
        => Ok(await _reports.IntegrationsAsync(Filter(null, null, null, dateFrom, dateTo), ct));

    /// <summary>Statik rapor kataloğu.</summary>
    [HttpGet("catalog")]
    [Authorize(Policy = Permissions.ReportRead)]
    public ActionResult<IEnumerable<ReportCatalogItemDto>> Catalog() => Ok(BuildCatalog());

    /// <summary>Bir raporu CSV dışa aktarır (mevcut filtreleri uygular; güvenlik kaydı bırakır).</summary>
    [HttpGet("{reportName}/export")]
    [Authorize(Policy = Permissions.ReportExport)]
    public async Task<IActionResult> Export(string reportName, [FromQuery] Guid? customerId, [FromQuery] Guid? projectId,
        [FromQuery] Guid? environmentId, [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo,
        [FromQuery] string format = "csv", CancellationToken ct = default)
    {
        if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Yalnızca 'csv' formatı desteklenir." });

        var f = Filter(customerId, projectId, environmentId, dateFrom, dateTo);
        var headers = new[] { "Metric", "Value" };
        List<IReadOnlyList<string?>> rows;

        switch (reportName.ToLowerInvariant())
        {
            case "overview":
                var o = await _reports.OverviewAsync(f, ct);
                rows = new()
                {
                    Row("TotalChanges", o.TotalChanges), Row("OpenChanges", o.OpenChanges), Row("ApprovedChanges", o.ApprovedChanges),
                    Row("ImplementedChanges", o.ImplementedChanges), Row("CancelledChanges", o.CancelledChanges),
                    Row("PendingApprovals", o.PendingApprovals), Row("ApprovedApprovals", o.ApprovedApprovals), Row("RejectedApprovals", o.RejectedApprovals),
                    Row("PlannedReleases", o.PlannedReleases), Row("ScheduledReleases", o.ScheduledReleases), Row("CompletedReleases", o.CompletedReleases), Row("AcceptedReleases", o.AcceptedReleases),
                    Row("RunningExecutions", o.RunningExecutions), Row("FailedExecutions", o.FailedExecutions), Row("RolledBackExecutions", o.RolledBackExecutions),
                    Row("PassedValidations", o.PassedValidations), Row("FailedValidations", o.FailedValidations),
                    Row("TotalDocuments", o.TotalDocuments), Row("UnreadNotifications", o.UnreadNotifications)
                };
                break;
            case "changes":
                rows = Buckets((await _reports.ChangesAsync(f, ct)).ChangesByStatus); break;
            case "approvals":
                rows = Buckets((await _reports.ApprovalsAsync(f, ct)).ApprovalsByStatus); break;
            case "releases":
                rows = Buckets((await _reports.ReleasesAsync(f, ct)).ReleasesByStatus); break;
            case "executions":
                rows = Buckets((await _reports.ExecutionsAsync(f, ct)).ExecutionsByStatus); break;
            case "validations":
                rows = Buckets((await _reports.ValidationsAsync(f, ct)).ValidationsByStatus); break;
            case "documents":
                rows = Buckets((await _reports.DocumentsAsync(f, ct)).DocumentsByCategory); break;
            case "security":
                if (!_currentUser.HasPermission(Permissions.AuditSecurityRead)) return Forbid();
                var s = await _reports.SecurityAsync(f, ct);
                rows = new()
                {
                    Row("SuccessfulLogins", s.SuccessfulLogins), Row("FailedLogins", s.FailedLogins), Row("Lockouts", s.Lockouts),
                    Row("PasswordChanges", s.PasswordChanges), Row("TokenRefreshes", s.TokenRefreshes), Row("Logouts", s.Logouts)
                };
                break;
            default:
                return NotFound(new { message = "Bilinmeyen rapor adı." });
        }

        var bytes = _export.ToCsv(headers, rows);
        await WriteExportAuditAsync($"'{reportName}' raporu CSV dışa aktarıldı.", ct);
        return File(bytes, "text/csv; charset=utf-8", $"report-{reportName}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    /* ── helpers ── */

    private static IReadOnlyList<string?> Row(string metric, int value) => new string?[] { metric, value.ToString() };
    private static List<IReadOnlyList<string?>> Buckets(IEnumerable<MetricBucketDto> buckets) =>
        buckets.Select(b => (IReadOnlyList<string?>)new string?[] { b.Key, b.Count.ToString() }).ToList();

    private async Task WriteExportAuditAsync(string description, CancellationToken ct)
    {
        _db.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(), UserId = _currentUser.UserId, Email = _currentUser.Email ?? string.Empty,
            EventType = SecurityEventTypes.ReportExported, Result = SecurityEventResults.Success,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(), UserAgent = Request.Headers.UserAgent.ToString(),
            Description = description, CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private static IReadOnlyList<ReportCatalogItemDto> BuildCatalog()
    {
        var filters = new List<string> { "dateFrom", "dateTo" };
        var scoped = new List<string> { "customerId", "projectId", "environmentId", "dateFrom", "dateTo" };
        var csv = new List<string> { "csv" };
        return new[]
        {
            new ReportCatalogItemDto { Code = "overview", Name = "Genel Bakış", Description = "Tüm domainler için özet metrikler.", RequiredPermission = Permissions.ReportRead, SupportedFilters = scoped, SupportedExportFormats = csv },
            new ReportCatalogItemDto { Code = "changes", Name = "Değişiklik Raporu", Description = "Durum/risk/sınıf/tür/ortam dağılımları ve süreler.", RequiredPermission = Permissions.ReportRead, SupportedFilters = scoped, SupportedExportFormats = csv },
            new ReportCatalogItemDto { Code = "approvals", Name = "Onay Raporu", Description = "Onay durumları, süreler, rol bazlı bekleyenler.", RequiredPermission = Permissions.ReportRead, SupportedFilters = filters, SupportedExportFormats = csv },
            new ReportCatalogItemDto { Code = "releases", Name = "Yayın Raporu", Description = "Yayın durum/tür/ortam ve hacim.", RequiredPermission = Permissions.ReportRead, SupportedFilters = scoped, SupportedExportFormats = csv },
            new ReportCatalogItemDto { Code = "executions", Name = "Yürütme Raporu", Description = "Başarı/başarısızlık/rollback oranları.", RequiredPermission = Permissions.ReportRead, SupportedFilters = filters, SupportedExportFormats = csv },
            new ReportCatalogItemDto { Code = "validations", Name = "Doğrulama Raporu", Description = "Geçme/kalma oranları ve süreler.", RequiredPermission = Permissions.ReportRead, SupportedFilters = filters, SupportedExportFormats = csv },
            new ReportCatalogItemDto { Code = "documents", Name = "Doküman Raporu", Description = "Kategori/durum, indirmeler, bütünlük.", RequiredPermission = Permissions.ReportRead, SupportedFilters = filters, SupportedExportFormats = csv },
            new ReportCatalogItemDto { Code = "security", Name = "Güvenlik Raporu", Description = "Giriş/kilit/parola olayları (Admin/Auditor).", RequiredPermission = Permissions.AuditSecurityRead, SupportedFilters = filters, SupportedExportFormats = csv },
            new ReportCatalogItemDto { Code = "integrations", Name = "Entegrasyon Raporu", Description = "Sağlayıcı/durum dağılımı, başarı/başarısızlık/retry oranları, ölü mektup.", RequiredPermission = Permissions.ReportRead, SupportedFilters = filters, SupportedExportFormats = csv },
        };
    }
}
