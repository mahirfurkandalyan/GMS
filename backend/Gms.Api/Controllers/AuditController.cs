using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Gms.Api.Services.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gms.Api.Controllers;

/// <summary>
/// Unified, read-only audit center over the domain audit tables (via the vw_UnifiedAuditRecords
/// view). Thin controller — query logic lives in <see cref="AuditReadService"/>. The domain
/// audit tables remain authoritative; nothing here mutates audit data.
/// </summary>
[ApiController]
[Route("api/audit")]
[Tags("Audit")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly AuditReadService _audit;
    private readonly IReportExportService _export;
    private readonly GmsDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AuditController(AuditReadService audit, IReportExportService export, GmsDbContext db, ICurrentUser currentUser)
    {
        _audit = audit;
        _export = export;
        _db = db;
        _currentUser = currentUser;
    }

    private AuditFilter BuildFilter(string? sourceModule, string? eventType, Guid? actorUserId, string? objectType,
        Guid? objectId, string? objectNumber, Guid? projectId, Guid? environmentId, string? result,
        DateTime? dateFrom, DateTime? dateTo, string? search) => new()
    {
        SourceModule = sourceModule, EventType = eventType, ActorUserId = actorUserId, ObjectType = objectType,
        ObjectId = objectId, ObjectNumber = objectNumber, ProjectId = projectId, EnvironmentId = environmentId,
        Result = result, DateFrom = dateFrom, DateTo = dateTo, Search = search
    };

    /// <summary>Birleşik, filtrelenebilir denetim zaman çizelgesi (tüm modüller).</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.AuditRead)]
    public async Task<ActionResult<PagedResult<UnifiedAuditRecordDto>>> GetAll(
        [FromQuery] string? sourceModule, [FromQuery] string? eventType, [FromQuery] Guid? actorUserId,
        [FromQuery] string? objectType, [FromQuery] Guid? objectId, [FromQuery] string? objectNumber,
        [FromQuery] Guid? projectId, [FromQuery] Guid? environmentId, [FromQuery] string? result,
        [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, [FromQuery] string? search,
        [FromQuery] PagedQuery paging, CancellationToken ct)
    {
        var filter = BuildFilter(sourceModule, eventType, actorUserId, objectType, objectId, objectNumber, projectId, environmentId, result, dateFrom, dateTo, search);
        return Ok(await _audit.QueryAsync(filter, paging, ct));
    }

    /// <summary>Bir nesnenin tam kronolojik geçmişi (Change/Approval/Release/Deployment/Validation/Document/Notification).</summary>
    [HttpGet("object/{objectType}/{objectId:guid}")]
    [Authorize(Policy = Permissions.AuditRead)]
    public async Task<ActionResult<IEnumerable<UnifiedAuditRecordDto>>> ObjectTimeline(string objectType, Guid objectId, CancellationToken ct)
        => Ok(await _audit.ObjectTimelineAsync(objectType, objectId, ct));

    /// <summary>Bir kullanıcının aktivite geçmişi.</summary>
    [HttpGet("user/{userId:guid}")]
    [Authorize(Policy = Permissions.AuditRead)]
    public async Task<ActionResult<PagedResult<UnifiedAuditRecordDto>>> UserActivity(Guid userId, [FromQuery] PagedQuery paging, CancellationToken ct)
        => Ok(await _audit.UserActivityAsync(userId, paging, ct));

    /// <summary>Güvenlik denetim listesi (yalnızca audit.security.read).</summary>
    [HttpGet("security")]
    [Authorize(Policy = Permissions.AuditSecurityRead)]
    public async Task<ActionResult<PagedResult<UnifiedAuditRecordDto>>> Security(
        [FromQuery] string? eventType, [FromQuery] string? result, [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo,
        [FromQuery] string? search, [FromQuery] PagedQuery paging, CancellationToken ct)
    {
        var filter = BuildFilter(null, eventType, null, null, null, null, null, null, result, dateFrom, dateTo, search);
        return Ok(await _audit.SecurityAsync(filter, paging, ct));
    }

    /// <summary>Denetim özet metrikleri.</summary>
    [HttpGet("summary")]
    [Authorize(Policy = Permissions.AuditRead)]
    public async Task<ActionResult<AuditSummaryDto>> Summary(
        [FromQuery] string? sourceModule, [FromQuery] Guid? projectId, [FromQuery] Guid? environmentId,
        [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, CancellationToken ct)
    {
        var filter = BuildFilter(sourceModule, null, null, null, null, null, projectId, environmentId, null, dateFrom, dateTo, null);
        return Ok(await _audit.SummaryAsync(filter, ct));
    }

    /// <summary>Denetim kayıtlarını CSV dışa aktarır (mevcut filtreleri uygular; güvenlik kaydı bırakır).</summary>
    [HttpGet("export")]
    [Authorize(Policy = Permissions.AuditExport)]
    public async Task<IActionResult> Export(
        [FromQuery] string? sourceModule, [FromQuery] string? eventType, [FromQuery] Guid? actorUserId,
        [FromQuery] string? objectType, [FromQuery] Guid? objectId, [FromQuery] string? objectNumber,
        [FromQuery] Guid? projectId, [FromQuery] Guid? environmentId, [FromQuery] string? result,
        [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, [FromQuery] string? search,
        [FromQuery] string format = "csv", CancellationToken ct = default)
    {
        if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Yalnızca 'csv' formatı desteklenir." });

        var filter = BuildFilter(sourceModule, eventType, actorUserId, objectType, objectId, objectNumber, projectId, environmentId, result, dateFrom, dateTo, search);
        var records = await _audit.ForExportAsync(filter, ct);
        var bytes = _export.AuditToCsv(records);

        await WriteExportAuditAsync(SecurityEventTypes.AuditExported, $"Denetim CSV dışa aktarıldı ({records.Count} kayıt).", ct);
        return File(bytes, "text/csv; charset=utf-8", $"audit-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    private async Task WriteExportAuditAsync(string eventType, string description, CancellationToken ct)
    {
        _db.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.UserId,
            Email = _currentUser.Email ?? string.Empty,
            EventType = eventType,
            Result = SecurityEventResults.Success,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            Description = description,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }
}
