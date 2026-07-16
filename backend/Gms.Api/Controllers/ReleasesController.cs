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
[Route("api/releases")]
[Tags("Releases")]
[Authorize]
public class ReleasesController : ControllerBase
{
    private readonly GmsDbContext _db;
    private readonly ReleasePlanningService _planning;
    private readonly ICurrentUser _currentUser;

    public ReleasesController(GmsDbContext db, ReleasePlanningService planning, ICurrentUser currentUser)
    {
        _db = db;
        _planning = planning;
        _currentUser = currentUser;
    }

    /// <summary>Filtrelenebilir + sayfalanabilir yayın listesi (özet).</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.ReleaseRead)]
    public async Task<ActionResult<PagedResult<ReleasePlanListDto>>> GetAll(
        [FromQuery] Guid? customerId, [FromQuery] Guid? projectId, [FromQuery] Guid? environmentId,
        [FromQuery] string? status, [FromQuery] string? search, [FromQuery] PagedQuery paging)
    {
        var query = _db.ReleasePlans.AsNoTracking().AsQueryable();

        if (customerId.HasValue) query = query.Where(r => r.CustomerId == customerId.Value);
        if (projectId.HasValue) query = query.Where(r => r.ProjectId == projectId.Value);
        if (environmentId.HasValue) query = query.Where(r => r.EnvironmentId == environmentId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(r => r.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(r => r.ReleaseNo.Contains(s) || r.Name.Contains(s));
        }

        var totalCount = await query.CountAsync();

        var ordered = (paging.SortBy?.ToLowerInvariant()) switch
        {
            "releaseno" => paging.Descending ? query.OrderByDescending(r => r.ReleaseNo) : query.OrderBy(r => r.ReleaseNo),
            "name" => paging.Descending ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name),
            "status" => paging.Descending ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
            "riskscore" => paging.Descending ? query.OrderByDescending(r => r.RiskScore) : query.OrderBy(r => r.RiskScore),
            _ => paging.Descending ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt)
        };

        var items = await ordered.ThenBy(r => r.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(r => new ReleasePlanListDto
            {
                Id = r.Id, ReleaseNo = r.ReleaseNo, Name = r.Name, Version = r.Version,
                CustomerName = r.Customer!.Name, ProjectName = r.Project!.Name, EnvironmentName = r.Environment!.Name,
                ReleaseType = r.ReleaseType, Status = r.Status, RiskLevel = r.RiskLevel, RiskScore = r.RiskScore,
                ChangeCount = r.Items.Count, TotalEstimatedMinutes = r.TotalEstimatedMinutes,
                PlannedDeploymentStart = r.PlannedDeploymentStart,
                ReleaseManagerName = r.ReleaseManagerUser!.FullName, CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(PagedResult<ReleasePlanListDto>.Create(items, paging.Page, paging.PageSize, totalCount));
    }

    /// <summary>Tam yayın detayı: değişiklikler, dağıtım planı, dokümanlar, denetim.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.ReleaseRead)]
    public async Task<ActionResult<ReleasePlanDetailDto>> GetById(Guid id)
    {
        var plan = await LoadFull(id);
        if (plan is null) return NotFound(new { message = "Yayın bulunamadı." });
        return Ok(await MapDetail(plan));
    }

    /// <summary>Yayın denetim olaylarını döndürür.</summary>
    [HttpGet("{id:guid}/audit")]
    [Authorize(Policy = Permissions.ReleaseRead)]
    public async Task<ActionResult<IEnumerable<ReleaseAuditEventDto>>> GetAudit(Guid id)
    {
        if (!await _db.ReleasePlans.AnyAsync(r => r.Id == id))
            return NotFound(new { message = "Yayın bulunamadı." });

        var events = await _db.ReleaseAuditEvents.AsNoTracking()
            .Where(e => e.ReleasePlanId == id)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new ReleaseAuditEventDto
            {
                Id = e.Id, EventType = e.EventType, Description = e.Description, ActorUserId = e.ActorUserId,
                ActorUserName = _db.Users.Where(u => u.Id == e.ActorUserId).Select(u => u.FullName).FirstOrDefault() ?? string.Empty,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Onaylı değişikliklerden yeni yayın planı oluşturur (durum: Planned).</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.ReleaseCreate)]
    public async Task<ActionResult<ReleasePlanDetailDto>> Create([FromBody] CreateReleasePlanDto dto)
    {
        var result = await _planning.CreateAsync(dto, _currentUser.RequireUserId());
        if (result.Plan is null) return BadRequest(new { message = result.Error });

        await _db.SaveChangesAsync();

        var full = await LoadFull(result.Plan.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Plan.Id }, await MapDetail(full!));
    }

    /// <summary>Yayın genel bilgilerini ve dağıtım planını günceller.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.ReleaseUpdate)]
    public async Task<ActionResult<ReleasePlanDetailDto>> Update(Guid id, [FromBody] UpdateReleasePlanDto dto)
    {
        var actor = _currentUser.RequireUserId();
        var plan = await LoadFull(id);
        if (plan is null) return NotFound(new { message = "Yayın bulunamadı." });

        // Optimistic concurrency: enforce the client's token if supplied (→ 409 on mismatch).
        if (!string.IsNullOrWhiteSpace(dto.RowVersion))
            _db.Entry(plan).Property(p => p.RowVersion).OriginalValue = Convert.FromBase64String(dto.RowVersion);

        if (plan.Status is ReleaseStatuses.Completed or ReleaseStatuses.Cancelled)
            return BadRequest(new { message = "Tamamlanmış veya iptal edilmiş yayın güncellenemez." });
        if (dto.ReleaseType is not null && !ReleaseTypes.All.Contains(dto.ReleaseType))
            return BadRequest(new { message = "Geçersiz yayın türü." });

        if (!string.IsNullOrWhiteSpace(dto.Name)) plan.Name = dto.Name.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Version)) plan.Version = dto.Version.Trim();
        if (dto.ReleaseType is not null) plan.ReleaseType = dto.ReleaseType;
        if (dto.PlannedDeploymentStart.HasValue) plan.PlannedDeploymentStart = dto.PlannedDeploymentStart;
        if (dto.PlannedDeploymentEnd.HasValue) plan.PlannedDeploymentEnd = dto.PlannedDeploymentEnd;
        if (dto.RollbackWindow is not null) plan.RollbackWindow = dto.RollbackWindow.Trim();
        if (dto.BusinessOwner is not null) plan.BusinessOwner = dto.BusinessOwner.Trim();
        if (dto.TechnicalOwner is not null) plan.TechnicalOwner = dto.TechnicalOwner.Trim();
        if (dto.Description is not null) plan.Description = dto.Description.Trim();

        if (dto.DeploymentPlan is not null && plan.DeploymentPlan is not null)
        {
            var dp = dto.DeploymentPlan;
            if (dp.DeploymentStrategy is not null) plan.DeploymentPlan.DeploymentStrategy = dp.DeploymentStrategy.Trim();
            if (dp.CommunicationPlan is not null) plan.DeploymentPlan.CommunicationPlan = dp.CommunicationPlan.Trim();
            if (dp.RollbackStrategy is not null) plan.DeploymentPlan.RollbackStrategy = dp.RollbackStrategy.Trim();
            plan.DeploymentPlan.DowntimeExpected = dp.DowntimeExpected;
            plan.DeploymentPlan.EstimatedDowntimeMinutes = dp.EstimatedDowntimeMinutes;
            if (dp.Notes is not null) plan.DeploymentPlan.Notes = dp.Notes.Trim();
        }

        var now = DateTime.UtcNow;
        plan.UpdatedAt = now;
        plan.AuditEvents.Add(AuditFactory.Release(ReleaseAuditEventTypes.ReleaseUpdated, "Yayın güncellendi.", actor, now));

        await _db.SaveChangesAsync();
        return Ok(await MapDetail(plan));
    }

    /// <summary>Yayını zamanlar (Planned → Scheduled).</summary>
    [HttpPost("{id:guid}/schedule")]
    [Authorize(Policy = Permissions.ReleaseSchedule)]
    public Task<ActionResult<ReleasePlanDetailDto>> Schedule(Guid id) =>
        RunAction(id, (plan) => _planning.ScheduleAsync(plan, _currentUser.RequireUserId()));

    /// <summary>Yayını tamamlar (değişiklikler → Implemented).</summary>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = Permissions.ReleaseComplete)]
    public Task<ActionResult<ReleasePlanDetailDto>> Complete(Guid id) =>
        RunAction(id, (plan) => _planning.CompleteAsync(plan, _currentUser.RequireUserId()));

    /// <summary>Yayını iptal eder (Scheduled değişiklikler → Approved).</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Permissions.ReleaseCancel)]
    public Task<ActionResult<ReleasePlanDetailDto>> Cancel(Guid id) =>
        RunAction(id, (plan) => _planning.CancelAsync(plan, _currentUser.RequireUserId()));

    /* ── Private helpers ─────────────────────────────────── */

    private async Task<ActionResult<ReleasePlanDetailDto>> RunAction(
        Guid id, Func<ReleasePlan, Task<ReleaseActionResult>> action)
    {
        var plan = await LoadFull(id);
        if (plan is null) return NotFound(new { message = "Yayın bulunamadı." });

        var result = await action(plan);
        if (!result.Ok) return BadRequest(new { message = result.Error });

        await _db.SaveChangesAsync();
        var refreshed = await LoadFull(id);
        return Ok(await MapDetail(refreshed!));
    }

    private Task<ReleasePlan?> LoadFull(Guid id) =>
        _db.ReleasePlans
            .Include(r => r.Customer)
            .Include(r => r.Project)
            .Include(r => r.Environment)
            .Include(r => r.ReleaseManagerUser)
            .Include(r => r.DeploymentPlan)
            .Include(r => r.Items).ThenInclude(i => i.ChangeRequest)
            .Include(r => r.Documents)
            .Include(r => r.AuditEvents)
            .AsSplitQuery() // multiple collection includes → avoid Cartesian explosion
            .FirstOrDefaultAsync(r => r.Id == id);

    private async Task<ReleasePlanDetailDto> MapDetail(ReleasePlan r)
    {
        // Resolve audit actor display names in one query for the timeline.
        var actorIds = r.AuditEvents.Select(e => e.ActorUserId).Distinct().ToList();
        var actorNames = await _db.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        var dto = new ReleasePlanDetailDto
        {
            Id = r.Id, ReleaseNo = r.ReleaseNo, Name = r.Name, Version = r.Version,
            CustomerId = r.CustomerId, CustomerName = r.Customer?.Name ?? string.Empty,
            ProjectId = r.ProjectId, ProjectName = r.Project?.Name ?? string.Empty,
            EnvironmentId = r.EnvironmentId, EnvironmentName = r.Environment?.Name ?? string.Empty,
            ReleaseType = r.ReleaseType, Status = r.Status, RiskLevel = r.RiskLevel, RiskScore = r.RiskScore,
            TotalEstimatedMinutes = r.TotalEstimatedMinutes,
            PlannedDeploymentStart = r.PlannedDeploymentStart, PlannedDeploymentEnd = r.PlannedDeploymentEnd,
            RollbackWindow = r.RollbackWindow, BusinessOwner = r.BusinessOwner, TechnicalOwner = r.TechnicalOwner,
            ReleaseManagerUserId = r.ReleaseManagerUserId, ReleaseManagerName = r.ReleaseManagerUser?.FullName ?? string.Empty,
            Description = r.Description, CreatedAt = r.CreatedAt, UpdatedAt = r.UpdatedAt,
            RowVersion = r.RowVersion is { Length: > 0 } ? Convert.ToBase64String(r.RowVersion) : string.Empty,
            Items = r.Items.OrderBy(i => i.DeploymentOrder).Select(i => new ReleasePlanItemDto
            {
                Id = i.Id, ChangeRequestId = i.ChangeRequestId,
                ChangeNo = i.ChangeRequest?.ChangeNo ?? string.Empty, ChangeTitle = i.ChangeRequest?.Title ?? string.Empty,
                ChangeStatus = i.ChangeRequest?.Status ?? string.Empty, ChangeRiskLevel = i.ChangeRequest?.RiskLevel ?? string.Empty,
                DeploymentOrder = i.DeploymentOrder, EstimatedMinutes = i.EstimatedMinutes, RollbackRequired = i.RollbackRequired
            }).ToList(),
            DeploymentPlan = r.DeploymentPlan is null ? null : new ReleaseDeploymentPlanDto
            {
                DeploymentStrategy = r.DeploymentPlan.DeploymentStrategy, CommunicationPlan = r.DeploymentPlan.CommunicationPlan,
                RollbackStrategy = r.DeploymentPlan.RollbackStrategy, DowntimeExpected = r.DeploymentPlan.DowntimeExpected,
                EstimatedDowntimeMinutes = r.DeploymentPlan.EstimatedDowntimeMinutes, Notes = r.DeploymentPlan.Notes
            },
            Documents = r.Documents.OrderBy(d => d.CreatedAt).Select(d => new ReleaseDocumentDto
            {
                Id = d.Id, DocumentType = d.DocumentType, DocumentName = d.DocumentName, Version = d.Version, CreatedAt = d.CreatedAt
            }).ToList(),
            AuditEvents = r.AuditEvents.OrderByDescending(e => e.CreatedAt).Select(e => new ReleaseAuditEventDto
            {
                Id = e.Id, EventType = e.EventType, Description = e.Description, ActorUserId = e.ActorUserId,
                ActorUserName = actorNames.TryGetValue(e.ActorUserId, out var n) ? n : string.Empty,
                CreatedAt = e.CreatedAt
            }).ToList()
        };

        return dto;
    }
}
