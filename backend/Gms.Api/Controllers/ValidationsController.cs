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
[Route("api/validations")]
[Tags("Validations")]
[Authorize]
public class ValidationsController : ControllerBase
{
    private readonly GmsDbContext _db;
    private readonly ValidationService _validation;
    private readonly ICurrentUser _currentUser;

    public ValidationsController(GmsDbContext db, ValidationService validation, ICurrentUser currentUser)
    {
        _db = db;
        _validation = validation;
        _currentUser = currentUser;
    }

    /// <summary>Filtrelenebilir + sayfalanabilir doğrulama listesi (özet).</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.ValidationRead)]
    public async Task<ActionResult<PagedResult<ValidationRunListDto>>> GetAll(
        [FromQuery] Guid? deploymentRunId, [FromQuery] string? status, [FromQuery] string? validationType,
        [FromQuery] string? search, [FromQuery] PagedQuery paging)
    {
        var query = _db.ValidationRuns.AsNoTracking().AsQueryable();

        if (deploymentRunId.HasValue) query = query.Where(v => v.DeploymentRunId == deploymentRunId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(v => v.Status == status);
        if (!string.IsNullOrWhiteSpace(validationType)) query = query.Where(v => v.ValidationType == validationType);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(v => v.ValidationNo.Contains(s) || v.DeploymentRun!.ExecutionNo.Contains(s));
        }

        var totalCount = await query.CountAsync();

        var ordered = (paging.SortBy?.ToLowerInvariant()) switch
        {
            "validationno" => paging.Descending ? query.OrderByDescending(v => v.ValidationNo) : query.OrderBy(v => v.ValidationNo),
            "status" => paging.Descending ? query.OrderByDescending(v => v.Status) : query.OrderBy(v => v.Status),
            "startedat" => paging.Descending ? query.OrderByDescending(v => v.StartedAt) : query.OrderBy(v => v.StartedAt),
            _ => paging.Descending ? query.OrderByDescending(v => v.CreatedAt) : query.OrderBy(v => v.CreatedAt)
        };

        var items = await ordered.ThenBy(v => v.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(v => new ValidationRunListDto
            {
                Id = v.Id, ValidationNo = v.ValidationNo,
                DeploymentRunId = v.DeploymentRunId, ExecutionNo = v.DeploymentRun!.ExecutionNo,
                ReleaseNo = v.DeploymentRun!.ReleasePlan!.ReleaseNo,
                Status = v.Status, ValidationType = v.ValidationType, OverallResult = v.OverallResult,
                CheckCount = v.Checks.Count,
                PassedCheckCount = v.Checks.Count(c => c.Status == ValidationCheckStatuses.Passed),
                StartedAt = v.StartedAt, CompletedAt = v.CompletedAt,
                ValidatedByUserName = v.ValidatedByUser!.FullName, CreatedAt = v.CreatedAt
            })
            .ToListAsync();

        return Ok(PagedResult<ValidationRunListDto>.Create(items, paging.Page, paging.PageSize, totalCount));
    }

    /// <summary>Tam doğrulama detayı: kontroller (sıralı), kanıtlar ve denetim olayları.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.ValidationRead)]
    public async Task<ActionResult<ValidationRunDetailDto>> GetById(Guid id)
    {
        var run = await LoadFull(id);
        if (run is null) return NotFound(new { message = "Doğrulama bulunamadı." });
        return Ok(MapDetail(run));
    }

    /// <summary>Doğrulama denetim olaylarını döndürür.</summary>
    [HttpGet("{id:guid}/audit")]
    [Authorize(Policy = Permissions.ValidationRead)]
    public async Task<ActionResult<IEnumerable<ValidationEventDto>>> GetAudit(Guid id)
    {
        if (!await _db.ValidationRuns.AnyAsync(v => v.Id == id))
            return NotFound(new { message = "Doğrulama bulunamadı." });

        var events = await _db.ValidationEvents.AsNoTracking()
            .Where(e => e.ValidationRunId == id)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new ValidationEventDto { Id = e.Id, EventType = e.EventType, Description = e.Description, ActorUserId = e.ActorUserId, CreatedAt = e.CreatedAt })
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Tamamlanmış bir yürütme için doğrulama oluşturur (durum: Created).</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.ValidationCreate)]
    public async Task<ActionResult<ValidationRunDetailDto>> Create([FromBody] CreateValidationRunDto dto)
    {
        var result = await _validation.CreateAsync(dto, _currentUser.RequireUserId());
        if (result.Run is null) return BadRequest(new { message = result.Error });

        await _db.SaveChangesAsync();

        var full = await LoadFull(result.Run.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Run.Id }, MapDetail(full!));
    }

    /// <summary>Doğrulamayı başlatır (Created → Running).</summary>
    [HttpPost("{id:guid}/start")]
    [Authorize(Policy = Permissions.ValidationStart)]
    public Task<ActionResult<ValidationRunDetailDto>> Start(Guid id, [FromBody] ValidationActionDto dto) =>
        RunAction(id, dto, (run) => _validation.StartAsync(run, _currentUser.RequireUserId()));

    /// <summary>Sıradaki bekleyen kontrolü başlatır (tek aktif kontrol kuralı).</summary>
    [HttpPost("{id:guid}/start-next-check")]
    [Authorize(Policy = Permissions.ValidationCheckExecute)]
    public Task<ActionResult<ValidationRunDetailDto>> StartNextCheck(Guid id, [FromBody] ValidationActionDto dto) =>
        RunAction(id, dto, (run) => Task.FromResult(_validation.StartNextCheck(run, _currentUser.RequireUserId())));

    /// <summary>Aktif kontrolü geçirir; son kontrolse doğrulamayı başarıyla kapatır (yayın → Accepted).</summary>
    [HttpPost("{id:guid}/pass-check")]
    [Authorize(Policy = Permissions.ValidationCheckExecute)]
    public Task<ActionResult<ValidationRunDetailDto>> PassCheck(Guid id, [FromBody] ValidationActionDto dto) =>
        RunAction(id, dto, (run) => _validation.PassCheckAsync(run, _currentUser.RequireUserId(), dto.ActualResult, dto.Notes));

    /// <summary>Aktif kontrolü başarısız işaretler (doğrulama → Failed; yayın Completed kalır).</summary>
    [HttpPost("{id:guid}/fail-check")]
    [Authorize(Policy = Permissions.ValidationCheckExecute)]
    public Task<ActionResult<ValidationRunDetailDto>> FailCheck(Guid id, [FromBody] ValidationActionDto dto) =>
        RunAction(id, dto, (run) => _validation.FailCheckAsync(run, _currentUser.RequireUserId(), dto.ActualResult, dto.Notes));

    /* ── Private helpers ─────────────────────────────────── */

    private async Task<ActionResult<ValidationRunDetailDto>> RunAction(
        Guid id, ValidationActionDto dto, Func<ValidationRun, Task<ValidationActionResult>> action)
    {
        var run = await LoadFull(id);
        if (run is null) return NotFound(new { message = "Doğrulama bulunamadı." });

        // Optimistic concurrency: enforce the client's token if supplied (→ 409 on mismatch).
        if (!string.IsNullOrWhiteSpace(dto.RowVersion))
            _db.Entry(run).Property(r => r.RowVersion).OriginalValue = Convert.FromBase64String(dto.RowVersion);

        var result = await action(run);
        if (!result.Ok) return BadRequest(new { message = result.Error });

        await _db.SaveChangesAsync();

        var refreshed = await LoadFull(id);
        return Ok(MapDetail(refreshed!));
    }

    private Task<ValidationRun?> LoadFull(Guid id) =>
        _db.ValidationRuns
            .Include(r => r.ValidatedByUser)
            .Include(r => r.DeploymentRun).ThenInclude(d => d!.ReleasePlan)
            .Include(r => r.Checks).ThenInclude(c => c.ExecutedByUser)
            .Include(r => r.Evidence)
            .Include(r => r.Events)
            .AsSplitQuery() // multiple collection includes → avoid Cartesian explosion
            .FirstOrDefaultAsync(r => r.Id == id);

    private static ValidationRunDetailDto MapDetail(ValidationRun r) => new()
    {
        Id = r.Id, ValidationNo = r.ValidationNo,
        DeploymentRunId = r.DeploymentRunId, ExecutionNo = r.DeploymentRun?.ExecutionNo ?? string.Empty,
        ReleaseNo = r.DeploymentRun?.ReleasePlan?.ReleaseNo ?? string.Empty,
        ReleaseStatus = r.DeploymentRun?.ReleasePlan?.Status ?? string.Empty,
        Status = r.Status, ValidationType = r.ValidationType, OverallResult = r.OverallResult,
        StartedAt = r.StartedAt, CompletedAt = r.CompletedAt,
        ValidatedByUserId = r.ValidatedByUserId, ValidatedByUserName = r.ValidatedByUser?.FullName ?? string.Empty,
        Summary = r.Summary, CreatedAt = r.CreatedAt,
        RowVersion = r.RowVersion is { Length: > 0 } ? Convert.ToBase64String(r.RowVersion) : string.Empty,
        Checks = r.Checks.OrderBy(c => c.CheckOrder).Select(c => new ValidationCheckDto
        {
            Id = c.Id, CheckOrder = c.CheckOrder, Title = c.Title,
            ExpectedResult = c.ExpectedResult, ActualResult = c.ActualResult, Status = c.Status,
            ExecutedAt = c.ExecutedAt, ExecutedByUserId = c.ExecutedByUserId,
            ExecutedByUserName = c.ExecutedByUser?.FullName, Notes = c.Notes
        }).ToList(),
        Evidence = r.Evidence.OrderBy(e => e.CreatedAt).Select(e => new ValidationEvidenceDto
        {
            Id = e.Id, EvidenceType = e.EvidenceType, FileName = e.FileName,
            Version = e.Version, Description = e.Description, CreatedAt = e.CreatedAt
        }).ToList(),
        Events = r.Events.OrderByDescending(e => e.CreatedAt).Select(e => new ValidationEventDto
        {
            Id = e.Id, EventType = e.EventType, Description = e.Description, ActorUserId = e.ActorUserId, CreatedAt = e.CreatedAt
        }).ToList()
    };
}
