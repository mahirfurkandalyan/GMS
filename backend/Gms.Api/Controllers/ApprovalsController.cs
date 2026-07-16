using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Gms.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Controllers;

[ApiController]
[Route("api/approvals")]
[Tags("Approvals")]
[Authorize]
public class ApprovalsController : ControllerBase
{
    private readonly GmsDbContext _db;
    private readonly ApprovalService _approval;

    public ApprovalsController(GmsDbContext db, ApprovalService approval)
    {
        _db = db;
        _approval = approval;
    }

    /// <summary>Filtrelenebilir + sayfalanabilir onay listesi (özet).</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.ApprovalRead)]
    public async Task<ActionResult<PagedResult<ApprovalRequestListDto>>> GetAll(
        [FromQuery] string? status, [FromQuery] string? relatedObjectType, [FromQuery] Guid? relatedObjectId,
        [FromQuery] Guid? requestedByUserId, [FromQuery] Guid? approverUserId, [FromQuery] string? approverRole,
        [FromQuery] string? search, [FromQuery] PagedQuery paging)
    {
        var query = _db.ApprovalRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status);
        if (!string.IsNullOrWhiteSpace(relatedObjectType)) query = query.Where(a => a.RelatedObjectType == relatedObjectType);
        if (relatedObjectId.HasValue) query = query.Where(a => a.RelatedObjectId == relatedObjectId.Value);
        if (requestedByUserId.HasValue) query = query.Where(a => a.RequestedByUserId == requestedByUserId.Value);
        if (approverUserId.HasValue) query = query.Where(a => a.Steps.Any(s => s.ApproverUserId == approverUserId.Value));
        if (!string.IsNullOrWhiteSpace(approverRole)) query = query.Where(a => a.Steps.Any(s => s.ApproverRole == approverRole));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(a => a.ApprovalNo.Contains(s) || a.Title.Contains(s));
        }

        var totalCount = await query.CountAsync();

        var ordered = (paging.SortBy?.ToLowerInvariant()) switch
        {
            "approvalno" => paging.Descending ? query.OrderByDescending(a => a.ApprovalNo) : query.OrderBy(a => a.ApprovalNo),
            "title" => paging.Descending ? query.OrderByDescending(a => a.Title) : query.OrderBy(a => a.Title),
            "status" => paging.Descending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            _ => paging.Descending ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt)
        };

        var items = await ordered.ThenBy(a => a.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(a => new ApprovalRequestListDto
            {
                Id = a.Id, ApprovalNo = a.ApprovalNo,
                RelatedObjectType = a.RelatedObjectType, RelatedObjectId = a.RelatedObjectId,
                Title = a.Title, Status = a.Status, Priority = a.Priority,
                RequestedByUserName = a.RequestedByUser!.FullName,
                RequestedAt = a.RequestedAt, CompletedAt = a.CompletedAt,
                StepCount = a.Steps.Count,
                CurrentStepNo = a.Steps.Where(s => s.Status == ApprovalStepStatuses.Active).Select(s => s.StepNo).FirstOrDefault(),
                CurrentStepName = a.Steps.Where(s => s.Status == ApprovalStepStatuses.Active).Select(s => s.StepName).FirstOrDefault()
            })
            .ToListAsync();

        return Ok(PagedResult<ApprovalRequestListDto>.Create(items, paging.Page, paging.PageSize, totalCount));
    }

    /// <summary>Tam onay detayı: adımlar, kararlar, denetim ve ilgili nesne özeti.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.ApprovalRead)]
    public async Task<ActionResult<ApprovalRequestDetailDto>> GetById(Guid id)
    {
        var approval = await LoadFull(id);
        if (approval is null) return NotFound(new { message = "Onay talebi bulunamadı." });
        return Ok(await MapDetail(approval));
    }

    /// <summary>Bir değişikliğe ait onay talebini döndürür (varsa).</summary>
    [HttpGet("by-change/{changeRequestId:guid}")]
    [Authorize(Policy = Permissions.ApprovalRead)]
    public async Task<ActionResult<ApprovalRequestDetailDto>> GetByChange(Guid changeRequestId)
    {
        var approval = await _db.ApprovalRequests
            .Include(a => a.RequestedByUser)
            .Include(a => a.Steps).ThenInclude(s => s.ApproverUser)
            .Include(a => a.Decisions).ThenInclude(d => d.SignedByUser)
            .Include(a => a.AuditEvents)
            .AsSplitQuery()
            .Where(a => a.RelatedObjectType == ApprovalRelatedObjectTypes.ChangeRequest && a.RelatedObjectId == changeRequestId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        if (approval is null) return NotFound(new { message = "Bu değişikliğe ait onay talebi bulunamadı." });
        return Ok(await MapDetail(approval));
    }

    /// <summary>Onay denetim olaylarını döndürür.</summary>
    [HttpGet("{id:guid}/audit")]
    [Authorize(Policy = Permissions.ApprovalRead)]
    public async Task<ActionResult<IEnumerable<ApprovalAuditEventDto>>> GetAudit(Guid id)
    {
        if (!await _db.ApprovalRequests.AnyAsync(a => a.Id == id))
            return NotFound(new { message = "Onay talebi bulunamadı." });

        var events = await _db.ApprovalAuditEvents.AsNoTracking()
            .Where(e => e.ApprovalRequestId == id)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new ApprovalAuditEventDto { Id = e.Id, EventType = e.EventType, Description = e.Description, ActorUserId = e.ActorUserId, CreatedAt = e.CreatedAt })
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Aktif adımı onaylar; zinciri ilerletir. Rol/izin ApprovalService'te zorlanır.</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize]
    public Task<ActionResult<ApprovalRequestDetailDto>> Approve(Guid id, [FromBody] ApprovalActionDto dto) =>
        RunAction(id, (approval) => _approval.ApproveAsync(approval, dto.Comment ?? string.Empty, dto.SignatureMeaning ?? string.Empty));

    /// <summary>Aktif adımı reddeder (yorum zorunlu).</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = Permissions.ApprovalReject)]
    public Task<ActionResult<ApprovalRequestDetailDto>> Reject(Guid id, [FromBody] ApprovalActionDto dto) =>
        RunAction(id, (approval) => _approval.RejectAsync(approval, dto.Comment ?? string.Empty, dto.SignatureMeaning ?? string.Empty));

    /// <summary>Revizyon talep eder (yorum zorunlu).</summary>
    [HttpPost("{id:guid}/request-revision")]
    [Authorize(Policy = Permissions.ApprovalRequestRevision)]
    public Task<ActionResult<ApprovalRequestDetailDto>> RequestRevision(Guid id, [FromBody] ApprovalActionDto dto) =>
        RunAction(id, (approval) => _approval.RequestRevisionAsync(approval, dto.Comment ?? string.Empty, dto.SignatureMeaning ?? string.Empty));

    /* ── Private helpers ─────────────────────────────────── */

    private async Task<ActionResult<ApprovalRequestDetailDto>> RunAction(
        Guid id, Func<ApprovalRequest, Task<ApprovalActionResult>> action)
    {

        var approval = await LoadFull(id);
        if (approval is null) return NotFound(new { message = "Onay talebi bulunamadı." });

        var result = await action(approval);
        if (!result.Ok) return BadRequest(new { message = result.Error });

        await _db.SaveChangesAsync();

        // Reload to reflect newly-added rows (decisions/audit) with navigations.
        var refreshed = await LoadFull(id);
        return Ok(await MapDetail(refreshed!));
    }

    private Task<ApprovalRequest?> LoadFull(Guid id) =>
        _db.ApprovalRequests
            .Include(a => a.RequestedByUser)
            .Include(a => a.Steps).ThenInclude(s => s.ApproverUser)
            .Include(a => a.Decisions).ThenInclude(d => d.SignedByUser)
            .Include(a => a.AuditEvents)
            .AsSplitQuery() // multiple collection includes → avoid Cartesian explosion
            .FirstOrDefaultAsync(a => a.Id == id);

    private async Task<ApprovalRequestDetailDto> MapDetail(ApprovalRequest a)
    {
        var stepNoById = a.Steps.ToDictionary(s => s.Id, s => s.StepNo);

        var dto = new ApprovalRequestDetailDto
        {
            Id = a.Id, ApprovalNo = a.ApprovalNo,
            RelatedObjectType = a.RelatedObjectType, RelatedObjectId = a.RelatedObjectId,
            Title = a.Title, Description = a.Description, Status = a.Status, Priority = a.Priority,
            RequestedByUserId = a.RequestedByUserId, RequestedByUserName = a.RequestedByUser?.FullName ?? string.Empty,
            RequestedAt = a.RequestedAt, CompletedAt = a.CompletedAt, CreatedAt = a.CreatedAt, UpdatedAt = a.UpdatedAt,
            RowVersion = a.RowVersion is { Length: > 0 } ? Convert.ToBase64String(a.RowVersion) : string.Empty,
            Steps = a.Steps.OrderBy(s => s.StepNo).Select(s => new ApprovalStepDto
            {
                Id = s.Id, StepNo = s.StepNo, StepName = s.StepName, ApproverRole = s.ApproverRole,
                ApproverUserId = s.ApproverUserId, ApproverUserName = s.ApproverUser?.FullName,
                Status = s.Status, DueDate = s.DueDate, CompletedAt = s.CompletedAt
            }).ToList(),
            Decisions = a.Decisions.OrderBy(d => d.CreatedAt).Select(d => new ApprovalDecisionDto
            {
                Id = d.Id, ApprovalStepId = d.ApprovalStepId, StepNo = stepNoById.TryGetValue(d.ApprovalStepId, out var n) ? n : 0,
                Decision = d.Decision, Comment = d.Comment, SignatureMeaning = d.SignatureMeaning,
                SignedByUserId = d.SignedByUserId, SignedByUserName = d.SignedByUser?.FullName ?? string.Empty, SignedAt = d.SignedAt
            }).ToList(),
            AuditEvents = a.AuditEvents.OrderByDescending(e => e.CreatedAt).Select(e => new ApprovalAuditEventDto
            {
                Id = e.Id, EventType = e.EventType, Description = e.Description, ActorUserId = e.ActorUserId, CreatedAt = e.CreatedAt
            }).ToList(),
            RelatedObject = await BuildRelatedSummary(a)
        };

        return dto;
    }

    private async Task<RelatedObjectSummaryDto?> BuildRelatedSummary(ApprovalRequest a)
    {
        if (a.RelatedObjectType != ApprovalRelatedObjectTypes.ChangeRequest) return null;

        return await _db.ChangeRequests.AsNoTracking()
            .Where(c => c.Id == a.RelatedObjectId)
            .Select(c => new RelatedObjectSummaryDto
            {
                Type = ApprovalRelatedObjectTypes.ChangeRequest, Id = c.Id,
                Code = c.ChangeNo, Title = c.Title, Status = c.Status, RiskLevel = c.RiskLevel
            })
            .FirstOrDefaultAsync();
    }
}
