using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Gms.Api.Services;
using Gms.Api.Services.Workflow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Controllers;

/// <summary>
/// Workflow RUNTIME: browse instances, act on assigned tasks (complete/reject), and manage the
/// instance lifecycle (start-for-change/cancel/pause/resume). Thin controller — the engine lives
/// in <see cref="WorkflowRuntimeService"/>.
/// </summary>
[ApiController]
[Route("api/workflow-instances")]
[Tags("WorkflowInstances")]
[Authorize]
public class WorkflowInstancesController : ControllerBase
{
    private readonly GmsDbContext _db;
    private readonly WorkflowRuntimeService _runtime;
    private readonly ChangeReadinessService _readiness;
    private readonly ICurrentUser _currentUser;

    public WorkflowInstancesController(GmsDbContext db, WorkflowRuntimeService runtime,
        ChangeReadinessService readiness, ICurrentUser currentUser)
    {
        _db = db;
        _runtime = runtime;
        _readiness = readiness;
        _currentUser = currentUser;
    }

    /// <summary>Filtrelenebilir + sayfalanabilir workflow örneği listesi.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.WorkflowInstanceRead)]
    public async Task<ActionResult<PagedResult<WorkflowInstanceListDto>>> GetAll(
        [FromQuery] Guid? definitionId, [FromQuery] string? status, [FromQuery] Guid? triggerObjectId,
        [FromQuery] string? search, [FromQuery] PagedQuery paging)
    {
        var query = _db.WorkflowInstances.AsNoTracking().AsQueryable();

        if (definitionId.HasValue) query = query.Where(i => i.WorkflowDefinitionId == definitionId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(i => i.Status == status);
        if (triggerObjectId.HasValue) query = query.Where(i => i.TriggerObjectId == triggerObjectId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(i => i.InstanceNo.Contains(s) || (i.TriggerObjectNumber != null && i.TriggerObjectNumber.Contains(s)));
        }

        var totalCount = await query.CountAsync();
        var ordered = (paging.SortBy?.ToLowerInvariant()) switch
        {
            "instanceno" => paging.Descending ? query.OrderByDescending(i => i.InstanceNo) : query.OrderBy(i => i.InstanceNo),
            "status" => paging.Descending ? query.OrderByDescending(i => i.Status) : query.OrderBy(i => i.Status),
            _ => paging.Descending ? query.OrderByDescending(i => i.CreatedAt) : query.OrderBy(i => i.CreatedAt)
        };

        var items = await ordered.ThenBy(i => i.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(i => new WorkflowInstanceListDto
            {
                Id = i.Id, InstanceNo = i.InstanceNo, WorkflowDefinitionId = i.WorkflowDefinitionId,
                WorkflowName = i.WorkflowDefinition!.Name, TriggerObjectType = i.TriggerObjectType,
                TriggerObjectId = i.TriggerObjectId, TriggerObjectNumber = i.TriggerObjectNumber, Status = i.Status,
                CurrentStepName = i.StepInstances.Where(s => s.Id == i.CurrentStepInstanceId).Select(s => s.Name).FirstOrDefault(),
                CreatedAt = i.CreatedAt, CompletedAt = i.CompletedAt
            })
            .ToListAsync();

        return Ok(PagedResult<WorkflowInstanceListDto>.Create(items, paging.Page, paging.PageSize, totalCount));
    }

    /// <summary>Tam workflow örneği detayı (adım örnekleri + olaylar).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.WorkflowInstanceRead)]
    public async Task<ActionResult<WorkflowInstanceDetailDto>> GetById(Guid id)
    {
        var instance = await LoadFull(id);
        if (instance is null) return NotFound(new { message = "Workflow örneği bulunamadı." });
        return Ok(await ToDetailAsync(instance));
    }

    /// <summary>Geçerli kullanıcıya atanmış, işlem bekleyen görevler.</summary>
    [HttpGet("tasks/mine")]
    [Authorize(Policy = Permissions.WorkflowTaskRead)]
    public async Task<ActionResult<IEnumerable<WorkflowTaskDto>>> MyTasks()
    {
        var me = _currentUser.RequireUserId();
        var roles = _currentUser.Roles.ToList();
        var now = DateTime.UtcNow;

        var steps = await _db.WorkflowStepInstances.AsNoTracking()
            .Where(s => s.Status == WorkflowStepStatuses.Active
                && s.WorkflowInstance!.Status == WorkflowInstanceStatuses.Waiting)
            .Where(s => s.AssignedUserId == me
                || (s.AssignedUserId == null && s.AssignedRole != null && roles.Contains(s.AssignedRole)))
            .OrderBy(s => s.DueAt ?? DateTime.MaxValue)
            .Select(s => new WorkflowTaskDto
            {
                InstanceId = s.WorkflowInstanceId, InstanceNo = s.WorkflowInstance!.InstanceNo,
                StepInstanceId = s.Id, StepKey = s.StepKey, StepName = s.Name, StepType = s.StepType,
                WorkflowName = s.WorkflowInstance.WorkflowDefinition!.Name,
                TriggerObjectType = s.WorkflowInstance.TriggerObjectType, TriggerObjectId = s.WorkflowInstance.TriggerObjectId,
                TriggerObjectNumber = s.WorkflowInstance.TriggerObjectNumber,
                AssignedRole = s.AssignedRole, AssignedUserId = s.AssignedUserId, DueAt = s.DueAt,
                IsOverdue = s.DueAt != null && s.DueAt < now, CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return Ok(steps);
    }

    /// <summary>Aktif manuel/onay adımını tamamlar (onay) ve motoru sürdürür.</summary>
    [HttpPost("{id:guid}/tasks/complete")]
    [Authorize(Policy = Permissions.WorkflowTaskComplete)]
    public async Task<ActionResult<WorkflowInstanceDetailDto>> CompleteTask(Guid id, [FromBody] WorkflowTaskActionDto dto)
    {
        await _runtime.CompleteTaskAsync(id, dto.Comment);
        var full = await LoadFull(id);
        return Ok(await ToDetailAsync(full!));
    }

    /// <summary>Aktif onay adımını reddeder; örneği sonlandırır ve değişikliği geri gönderir.</summary>
    [HttpPost("{id:guid}/tasks/reject")]
    [Authorize(Policy = Permissions.WorkflowTaskReject)]
    public async Task<ActionResult<WorkflowInstanceDetailDto>> RejectTask(Guid id, [FromBody] WorkflowTaskActionDto dto)
    {
        await _runtime.RejectTaskAsync(id, dto.Comment);
        var full = await LoadFull(id);
        return Ok(await ToDetailAsync(full!));
    }

    /// <summary>Submitted durumundaki bir değişiklik için (aktif örneği yoksa) workflow başlatır.</summary>
    [HttpPost("changes/{changeId:guid}/start")]
    [Authorize(Policy = Permissions.WorkflowInstanceStart)]
    public async Task<ActionResult<WorkflowInstanceDetailDto>> StartForChange(Guid changeId)
    {
        var change = await _db.ChangeRequests
            .Include(c => c.Environment).Include(c => c.Revisions).Include(c => c.Assets)
            .Include(c => c.Documents).Include(c => c.AuditEvents)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == changeId);
        if (change is null) return NotFound(new { message = "Değişiklik bulunamadı." });
        if (change.Status != ChangeStatuses.Submitted)
            return BadRequest(new { message = "Yalnızca Submitted durumundaki değişiklik için workflow başlatılabilir." });

        var hasActive = await _db.WorkflowInstances.AnyAsync(i => i.TriggerObjectId == changeId
            && (i.Status == WorkflowInstanceStatuses.Running || i.Status == WorkflowInstanceStatuses.Waiting
                || i.Status == WorkflowInstanceStatuses.Created));
        if (hasActive) return BadRequest(new { message = "Bu değişiklik için zaten devam eden bir workflow var." });

        var actor = _currentUser.RequireUserId();
        var readinessScore = EvaluateReadinessScore(change);
        var instance = await _runtime.StartForChangeAsync(change, readinessScore, actor);
        await _db.SaveChangesAsync();

        var full = await LoadFull(instance.Id);
        return Ok(await ToDetailAsync(full!));
    }

    /// <summary>Çalışan/bekleyen örneği iptal eder; değişikliği Submitted'a geri gönderir.</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Permissions.WorkflowInstanceCancel)]
    public async Task<ActionResult<WorkflowInstanceDetailDto>> Cancel(Guid id, [FromBody] WorkflowCancelDto dto)
    {
        await _runtime.CancelAsync(id, dto.Reason, dto.RowVersion);
        var full = await LoadFull(id);
        return Ok(await ToDetailAsync(full!));
    }

    /// <summary>Bekleyen örneği duraklatır (yönetici tutması).</summary>
    [HttpPost("{id:guid}/pause")]
    [Authorize(Policy = Permissions.WorkflowInstancePause)]
    public async Task<ActionResult<WorkflowInstanceDetailDto>> Pause(Guid id)
    {
        await _runtime.PauseAsync(id);
        var full = await LoadFull(id);
        return Ok(await ToDetailAsync(full!));
    }

    /// <summary>Duraklatılmış örneği aktif adımı üzerinde sürdürür.</summary>
    [HttpPost("{id:guid}/resume")]
    [Authorize(Policy = Permissions.WorkflowInstanceResume)]
    public async Task<ActionResult<WorkflowInstanceDetailDto>> Resume(Guid id)
    {
        await _runtime.ResumeAsync(id);
        var full = await LoadFull(id);
        return Ok(await ToDetailAsync(full!));
    }

    /* ── helpers ── */

    private int EvaluateReadinessScore(ChangeRequest c)
    {
        var latest = c.Revisions.Count > 0 ? c.Revisions.OrderByDescending(r => r.RevisionNo).First() : null;
        var input = new ChangeReadinessInput(
            HasBusinessReason: !string.IsNullOrWhiteSpace(c.BusinessReason),
            HasEnvironment: c.EnvironmentId != Guid.Empty,
            AssetCount: c.Assets.Count,
            ChangeType: c.ChangeType,
            HasRollbackScript: !string.IsNullOrWhiteSpace(latest?.RollbackScript),
            DocumentCount: c.Documents.Count,
            HasPlannedDate: c.PlannedImplementationDate.HasValue);
        return _readiness.Evaluate(input).ReadinessScore;
    }

    private Task<WorkflowInstance?> LoadFull(Guid id) =>
        _db.WorkflowInstances
            .Include(i => i.WorkflowDefinition)
            .Include(i => i.WorkflowVersion)
            .Include(i => i.StepInstances)
            .Include(i => i.Events)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id);

    /// <summary>Maps an instance to the detail DTO, resolving event actor display names from Users.</summary>
    private async Task<WorkflowInstanceDetailDto> ToDetailAsync(WorkflowInstance i)
    {
        var actorIds = i.Events.Select(e => e.ActorUserId).Distinct().ToList();
        var names = await _db.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);
        return MapDetail(i, names);
    }

    private static WorkflowInstanceDetailDto MapDetail(WorkflowInstance i, IReadOnlyDictionary<Guid, string>? actorNames = null) => new()
    {
        Id = i.Id, InstanceNo = i.InstanceNo, WorkflowDefinitionId = i.WorkflowDefinitionId,
        WorkflowCode = i.WorkflowDefinition?.Code ?? string.Empty, WorkflowName = i.WorkflowDefinition?.Name ?? string.Empty,
        WorkflowVersionId = i.WorkflowVersionId, VersionNumber = i.WorkflowVersion?.VersionNumber ?? 0,
        TriggerObjectType = i.TriggerObjectType, TriggerObjectId = i.TriggerObjectId, TriggerObjectNumber = i.TriggerObjectNumber,
        Status = i.Status, CurrentStepInstanceId = i.CurrentStepInstanceId, Outcome = i.Outcome,
        CreatedAt = i.CreatedAt, StartedAt = i.StartedAt, CompletedAt = i.CompletedAt,
        RowVersion = i.RowVersion is { Length: > 0 } ? Convert.ToBase64String(i.RowVersion) : string.Empty,
        Steps = i.StepInstances.OrderBy(s => s.StepOrder).ThenBy(s => s.CreatedAt).Select(s => new WorkflowStepInstanceDto
        {
            Id = s.Id, StepKey = s.StepKey, Name = s.Name, StepType = s.StepType, StepOrder = s.StepOrder,
            Status = s.Status, AssignedRole = s.AssignedRole, AssignedUserId = s.AssignedUserId, DueAt = s.DueAt,
            ActionedByUserId = s.ActionedByUserId, Result = s.Result, Comment = s.Comment,
            CreatedAt = s.CreatedAt, ActivatedAt = s.ActivatedAt, CompletedAt = s.CompletedAt
        }).ToList(),
        Events = i.Events.OrderByDescending(e => e.CreatedAt).Select(e => new WorkflowEventDto
        {
            Id = e.Id, WorkflowStepInstanceId = e.WorkflowStepInstanceId, EventType = e.EventType,
            Description = e.Description, ActorUserId = e.ActorUserId,
            ActorUserName = actorNames != null && actorNames.TryGetValue(e.ActorUserId, out var n) ? n : string.Empty,
            CreatedAt = e.CreatedAt
        }).ToList()
    };
}
