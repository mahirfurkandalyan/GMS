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
[Route("api/deployments")]
[Tags("Deployments")]
[Authorize]
public class DeploymentsController : ControllerBase
{
    private readonly GmsDbContext _db;
    private readonly ExecutionService _execution;
    private readonly ICurrentUser _currentUser;

    public DeploymentsController(GmsDbContext db, ExecutionService execution, ICurrentUser currentUser)
    {
        _db = db;
        _execution = execution;
        _currentUser = currentUser;
    }

    /// <summary>Filtrelenebilir + sayfalanabilir yürütme listesi (özet).</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.ExecutionRead)]
    public async Task<ActionResult<PagedResult<DeploymentRunListDto>>> GetAll(
        [FromQuery] Guid? releasePlanId, [FromQuery] string? status, [FromQuery] string? search,
        [FromQuery] PagedQuery paging)
    {
        var query = _db.DeploymentRuns.AsNoTracking().AsQueryable();

        if (releasePlanId.HasValue) query = query.Where(r => r.ReleasePlanId == releasePlanId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(r => r.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(r => r.ExecutionNo.Contains(s) || r.ReleasePlan!.Name.Contains(s));
        }

        var totalCount = await query.CountAsync();

        var ordered = (paging.SortBy?.ToLowerInvariant()) switch
        {
            "executionno" => paging.Descending ? query.OrderByDescending(r => r.ExecutionNo) : query.OrderBy(r => r.ExecutionNo),
            "status" => paging.Descending ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
            "startedat" => paging.Descending ? query.OrderByDescending(r => r.StartedAt) : query.OrderBy(r => r.StartedAt),
            _ => paging.Descending ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt)
        };

        var items = await ordered.ThenBy(r => r.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(r => new DeploymentRunListDto
            {
                Id = r.Id, ExecutionNo = r.ExecutionNo,
                ReleasePlanId = r.ReleasePlanId, ReleaseNo = r.ReleasePlan!.ReleaseNo, ReleaseName = r.ReleasePlan!.Name,
                Status = r.Status, OverallResult = r.OverallResult,
                StepCount = r.Steps.Count,
                CompletedStepCount = r.Steps.Count(s => s.Status == DeploymentStepStatuses.Completed),
                StartedAt = r.StartedAt, CompletedAt = r.CompletedAt,
                ExecutedByUserName = r.ExecutedByUser!.FullName, CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(PagedResult<DeploymentRunListDto>.Create(items, paging.Page, paging.PageSize, totalCount));
    }

    /// <summary>Tam yürütme detayı: adımlar (sıralı) ve denetim olayları.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.ExecutionRead)]
    public async Task<ActionResult<DeploymentRunDetailDto>> GetById(Guid id)
    {
        var run = await LoadFull(id);
        if (run is null) return NotFound(new { message = "Yürütme bulunamadı." });
        return Ok(MapDetail(run));
    }

    /// <summary>Yürütme denetim olaylarını döndürür.</summary>
    [HttpGet("{id:guid}/audit")]
    [Authorize(Policy = Permissions.ExecutionRead)]
    public async Task<ActionResult<IEnumerable<DeploymentEventDto>>> GetAudit(Guid id)
    {
        if (!await _db.DeploymentRuns.AnyAsync(r => r.Id == id))
            return NotFound(new { message = "Yürütme bulunamadı." });

        var events = await _db.DeploymentEvents.AsNoTracking()
            .Where(e => e.DeploymentRunId == id)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new DeploymentEventDto { Id = e.Id, EventType = e.EventType, Description = e.Description, ActorUserId = e.ActorUserId, CreatedAt = e.CreatedAt })
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Zamanlanmış bir yayın için yürütme oluşturur (durum: Created).</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.ExecutionCreate)]
    public async Task<ActionResult<DeploymentRunDetailDto>> Create([FromBody] CreateDeploymentRunDto dto)
    {
        var result = await _execution.CreateAsync(dto, _currentUser.RequireUserId());
        if (result.Run is null) return BadRequest(new { message = result.Error });

        await _db.SaveChangesAsync();

        var full = await LoadFull(result.Run.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Run.Id }, MapDetail(full!));
    }

    /// <summary>Yürütmeyi başlatır (Created → Running; yayın Scheduled → InProgress).</summary>
    [HttpPost("{id:guid}/start")]
    [Authorize(Policy = Permissions.ExecutionStart)]
    public Task<ActionResult<DeploymentRunDetailDto>> Start(Guid id, [FromBody] DeploymentActionDto dto) =>
        RunAction(id, dto, (run) => _execution.StartAsync(run, _currentUser.RequireUserId()));

    /// <summary>Sıradaki bekleyen adımı başlatır (tek aktif adım kuralı).</summary>
    [HttpPost("{id:guid}/start-next-step")]
    [Authorize(Policy = Permissions.ExecutionStepStart)]
    public Task<ActionResult<DeploymentRunDetailDto>> StartNextStep(Guid id, [FromBody] DeploymentActionDto dto) =>
        RunAction(id, dto, (run) => Task.FromResult(_execution.StartNextStep(run, _currentUser.RequireUserId())));

    /// <summary>Aktif adımı tamamlar; son adımsa yürütmeyi başarıyla kapatır.</summary>
    [HttpPost("{id:guid}/complete-step")]
    [Authorize(Policy = Permissions.ExecutionStepComplete)]
    public Task<ActionResult<DeploymentRunDetailDto>> CompleteStep(Guid id, [FromBody] DeploymentActionDto dto) =>
        RunAction(id, dto, (run) => _execution.CompleteStepAsync(run, _currentUser.RequireUserId(), dto.Notes));

    /// <summary>Aktif adımı başarısız işaretler (yürütme → Failed; yayın InProgress kalır).</summary>
    [HttpPost("{id:guid}/fail-step")]
    [Authorize(Policy = Permissions.ExecutionStepFail)]
    public Task<ActionResult<DeploymentRunDetailDto>> FailStep(Guid id, [FromBody] DeploymentActionDto dto) =>
        RunAction(id, dto, (run) => _execution.FailStepAsync(run, _currentUser.RequireUserId(), dto.Notes));

    /// <summary>Başarısız yürütmeyi geri alır (yayın → Cancelled; değişiklikler → Approved).</summary>
    [HttpPost("{id:guid}/rollback")]
    [Authorize(Policy = Permissions.ExecutionRollback)]
    public Task<ActionResult<DeploymentRunDetailDto>> Rollback(Guid id, [FromBody] DeploymentActionDto dto) =>
        RunAction(id, dto, (run) => _execution.RollbackAsync(run, _currentUser.RequireUserId(), dto.Notes));

    /* ── Private helpers ─────────────────────────────────── */

    private async Task<ActionResult<DeploymentRunDetailDto>> RunAction(
        Guid id, DeploymentActionDto dto, Func<DeploymentRun, Task<ExecutionActionResult>> action)
    {
        var run = await LoadFull(id);
        if (run is null) return NotFound(new { message = "Yürütme bulunamadı." });

        // Optimistic concurrency: enforce the client's token if supplied (→ 409 on mismatch).
        if (!string.IsNullOrWhiteSpace(dto.RowVersion))
            _db.Entry(run).Property(r => r.RowVersion).OriginalValue = Convert.FromBase64String(dto.RowVersion);

        var result = await action(run);
        if (!result.Ok) return BadRequest(new { message = result.Error });

        await _db.SaveChangesAsync();

        var refreshed = await LoadFull(id);
        return Ok(MapDetail(refreshed!));
    }

    private Task<DeploymentRun?> LoadFull(Guid id) =>
        _db.DeploymentRuns
            .Include(r => r.ReleasePlan)
            .Include(r => r.ExecutedByUser)
            .Include(r => r.Steps).ThenInclude(s => s.ReleasePlanItem).ThenInclude(i => i!.ChangeRequest)
            .Include(r => r.Steps).ThenInclude(s => s.ExecutedByUser)
            .Include(r => r.Events)
            .AsSplitQuery() // multiple collection includes → avoid Cartesian explosion
            .FirstOrDefaultAsync(r => r.Id == id);

    private static DeploymentRunDetailDto MapDetail(DeploymentRun r) => new()
    {
        Id = r.Id, ExecutionNo = r.ExecutionNo,
        ReleasePlanId = r.ReleasePlanId, ReleaseNo = r.ReleasePlan?.ReleaseNo ?? string.Empty,
        ReleaseName = r.ReleasePlan?.Name ?? string.Empty, ReleaseStatus = r.ReleasePlan?.Status ?? string.Empty,
        Status = r.Status, OverallResult = r.OverallResult,
        StartedAt = r.StartedAt, CompletedAt = r.CompletedAt,
        ExecutedByUserId = r.ExecutedByUserId, ExecutedByUserName = r.ExecutedByUser?.FullName ?? string.Empty,
        Notes = r.Notes, CreatedAt = r.CreatedAt,
        RowVersion = r.RowVersion is { Length: > 0 } ? Convert.ToBase64String(r.RowVersion) : string.Empty,
        Steps = r.Steps.OrderBy(s => s.StepOrder).Select(s => new DeploymentStepDto
        {
            Id = s.Id, ReleasePlanItemId = s.ReleasePlanItemId, StepOrder = s.StepOrder, Title = s.Title,
            Status = s.Status, ExecutionResult = s.ExecutionResult, RollbackExecuted = s.RollbackExecuted,
            StartedAt = s.StartedAt, CompletedAt = s.CompletedAt,
            ExecutedByUserId = s.ExecutedByUserId, ExecutedByUserName = s.ExecutedByUser?.FullName,
            Notes = s.Notes,
            ChangeRequestId = s.ReleasePlanItem?.ChangeRequestId ?? Guid.Empty,
            ChangeNo = s.ReleasePlanItem?.ChangeRequest?.ChangeNo ?? string.Empty,
            ChangeTitle = s.ReleasePlanItem?.ChangeRequest?.Title ?? string.Empty
        }).ToList(),
        Events = r.Events.OrderByDescending(e => e.CreatedAt).Select(e => new DeploymentEventDto
        {
            Id = e.Id, EventType = e.EventType, Description = e.Description, ActorUserId = e.ActorUserId, CreatedAt = e.CreatedAt
        }).ToList()
    };
}
