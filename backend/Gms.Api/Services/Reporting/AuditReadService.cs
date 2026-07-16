using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Services.Reporting;

/// <summary>Filters for the unified audit query (all applied in SQL).</summary>
public sealed class AuditFilter
{
    public string? SourceModule { get; set; }
    public string? EventType { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ObjectType { get; set; }
    public Guid? ObjectId { get; set; }
    public string? ObjectNumber { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? EnvironmentId { get; set; }
    public string? Result { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Search { get; set; }
}

/// <summary>
/// Read-only query service over the unified audit view. All filtering/grouping happens in
/// SQL Server (AsNoTracking, projection before materialization). Never mutates audit data.
/// </summary>
public sealed class AuditReadService
{
    private const int ExportMaxRows = 50_000;

    private readonly GmsDbContext _db;

    public AuditReadService(GmsDbContext db) => _db = db;

    public async Task<PagedResult<UnifiedAuditRecordDto>> QueryAsync(AuditFilter filter, PagedQuery paging, CancellationToken ct = default)
    {
        var query = ApplyFilters(_db.UnifiedAuditRecords.AsNoTracking(), filter);
        var total = await query.CountAsync(ct);

        var ordered = (paging.SortBy?.ToLowerInvariant()) switch
        {
            "sourcemodule" => paging.Descending ? query.OrderByDescending(a => a.SourceModule) : query.OrderBy(a => a.SourceModule),
            "eventtype" => paging.Descending ? query.OrderByDescending(a => a.EventType) : query.OrderBy(a => a.EventType),
            _ => paging.Descending ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt)
        };

        var items = await Project(ordered.ThenByDescending(a => a.RecordId).Skip(paging.Skip).Take(paging.PageSize)).ToListAsync(ct);
        return PagedResult<UnifiedAuditRecordDto>.Create(items, paging.Page, paging.PageSize, total);
    }

    public Task<List<UnifiedAuditRecordDto>> ObjectTimelineAsync(string objectType, Guid objectId, CancellationToken ct = default) =>
        Project(_db.UnifiedAuditRecords.AsNoTracking()
            .Where(a => a.ObjectType == objectType && a.ObjectId == objectId)
            .OrderBy(a => a.CreatedAt).ThenBy(a => a.RecordId)).ToListAsync(ct);

    public async Task<PagedResult<UnifiedAuditRecordDto>> UserActivityAsync(Guid userId, PagedQuery paging, CancellationToken ct = default)
    {
        var query = _db.UnifiedAuditRecords.AsNoTracking().Where(a => a.ActorUserId == userId);
        var total = await query.CountAsync(ct);
        var items = await Project(query.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.RecordId)
            .Skip(paging.Skip).Take(paging.PageSize)).ToListAsync(ct);
        return PagedResult<UnifiedAuditRecordDto>.Create(items, paging.Page, paging.PageSize, total);
    }

    public async Task<PagedResult<UnifiedAuditRecordDto>> SecurityAsync(AuditFilter filter, PagedQuery paging, CancellationToken ct = default)
    {
        filter.SourceModule = NotificationModules.Security; // force SECURITY scope
        return await QueryAsync(filter, paging, ct);
    }

    /// <summary>Bounded, ordered set for CSV export (respects filters).</summary>
    public Task<List<UnifiedAuditRecordDto>> ForExportAsync(AuditFilter filter, CancellationToken ct = default) =>
        Project(ApplyFilters(_db.UnifiedAuditRecords.AsNoTracking(), filter)
            .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.RecordId).Take(ExportMaxRows)).ToListAsync(ct);

    public async Task<AuditSummaryDto> SummaryAsync(AuditFilter filter, CancellationToken ct = default)
    {
        var query = ApplyFilters(_db.UnifiedAuditRecords.AsNoTracking(), filter);
        var now = DateTime.UtcNow;
        var todayUtc = now.Date;
        var sevenDaysAgo = now.AddDays(-7);

        var total = await query.CountAsync(ct);
        var today = await query.CountAsync(a => a.CreatedAt >= todayUtc, ct);
        var last7 = await query.CountAsync(a => a.CreatedAt >= sevenDaysAgo, ct);
        var failed = await query.CountAsync(a => a.Result == "Failure" || a.EventType.Contains("Failed"), ct);
        var security = await query.CountAsync(a => a.SourceModule == NotificationModules.Security, ct);

        var byModule = await query.GroupBy(a => a.SourceModule)
            .Select(g => new MetricBucketDto { Key = g.Key, Count = g.Count() })
            .OrderByDescending(b => b.Count).ToListAsync(ct);

        var byType = await query.GroupBy(a => a.EventType)
            .Select(g => new MetricBucketDto { Key = g.Key, Count = g.Count() })
            .OrderByDescending(b => b.Count).Take(15).ToListAsync(ct);

        var topUsersRaw = await query.Where(a => a.ActorUserId != null)
            .GroupBy(a => a.ActorUserId)
            .Select(g => new { g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).Take(5).ToListAsync(ct);

        var userIds = topUsersRaw.Select(x => x.Key).ToList();
        var names = await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName }).ToListAsync(ct);
        var mostActive = topUsersRaw.Select(x => new ActorActivityDto
        {
            ActorUserId = x.Key,
            ActorFullName = names.FirstOrDefault(n => n.Id == x.Key)?.FullName ?? string.Empty,
            Count = x.Count
        }).ToList();

        return new AuditSummaryDto
        {
            TotalEvents = total, EventsToday = today, EventsLast7Days = last7,
            FailedEvents = failed, SecurityEvents = security,
            MostActiveUsers = mostActive, EventsByModule = byModule, EventsByType = byType
        };
    }

    /* ── helpers ── */

    private static IQueryable<UnifiedAuditRecord> ApplyFilters(IQueryable<UnifiedAuditRecord> q, AuditFilter f)
    {
        if (!string.IsNullOrWhiteSpace(f.SourceModule)) q = q.Where(a => a.SourceModule == f.SourceModule);
        if (!string.IsNullOrWhiteSpace(f.EventType)) q = q.Where(a => a.EventType == f.EventType);
        if (f.ActorUserId.HasValue) q = q.Where(a => a.ActorUserId == f.ActorUserId);
        if (!string.IsNullOrWhiteSpace(f.ObjectType)) q = q.Where(a => a.ObjectType == f.ObjectType);
        if (f.ObjectId.HasValue) q = q.Where(a => a.ObjectId == f.ObjectId);
        if (!string.IsNullOrWhiteSpace(f.ObjectNumber)) q = q.Where(a => a.ObjectNumber == f.ObjectNumber);
        if (f.ProjectId.HasValue) q = q.Where(a => a.RelatedProjectId == f.ProjectId);
        if (f.EnvironmentId.HasValue) q = q.Where(a => a.RelatedEnvironmentId == f.EnvironmentId);
        if (!string.IsNullOrWhiteSpace(f.Result)) q = q.Where(a => a.Result == f.Result);
        if (f.DateFrom.HasValue) q = q.Where(a => a.CreatedAt >= f.DateFrom.Value);
        if (f.DateTo.HasValue) q = q.Where(a => a.CreatedAt <= f.DateTo.Value);
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim();
            q = q.Where(a => a.Description.Contains(s) || (a.ObjectNumber != null && a.ObjectNumber.Contains(s)) || a.EventType.Contains(s));
        }
        return q;
    }

    private IQueryable<UnifiedAuditRecordDto> Project(IQueryable<UnifiedAuditRecord> q) =>
        q.Select(a => new UnifiedAuditRecordDto
        {
            RecordId = a.RecordId, SourceModule = a.SourceModule, SourceTable = a.SourceTable,
            EventType = a.EventType, Description = a.Description,
            ActorUserId = a.ActorUserId,
            ActorFullName = a.ActorUserId == null ? null : _db.Users.Where(u => u.Id == a.ActorUserId).Select(u => u.FullName).FirstOrDefault(),
            ActorEmail = a.ActorUserId == null ? null : _db.Users.Where(u => u.Id == a.ActorUserId).Select(u => u.Email).FirstOrDefault(),
            ObjectType = a.ObjectType, ObjectId = a.ObjectId, ObjectNumber = a.ObjectNumber,
            RelatedProjectId = a.RelatedProjectId, RelatedEnvironmentId = a.RelatedEnvironmentId,
            Result = a.Result, IpAddress = a.IpAddress, CreatedAt = a.CreatedAt
        });
}
