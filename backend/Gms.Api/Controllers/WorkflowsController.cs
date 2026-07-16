using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Gms.Api.Services.Workflow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Controllers;

/// <summary>
/// Workflow DEFINITION management: browse definitions/versions, author draft graphs, validate,
/// publish (immutable), activate, clone and archive. Thin controller — all rules live in
/// <see cref="WorkflowDefinitionService"/>.
/// </summary>
[ApiController]
[Route("api/workflows")]
[Tags("Workflows")]
[Authorize]
public class WorkflowsController : ControllerBase
{
    private readonly GmsDbContext _db;
    private readonly WorkflowDefinitionService _service;

    public WorkflowsController(GmsDbContext db, WorkflowDefinitionService service)
    {
        _db = db;
        _service = service;
    }

    /// <summary>Filtrelenebilir + sayfalanabilir workflow tanımı listesi.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.WorkflowDefinitionRead)]
    public async Task<ActionResult<PagedResult<WorkflowDefinitionListDto>>> GetAll(
        [FromQuery] string? category, [FromQuery] string? status, [FromQuery] string? changeClass,
        [FromQuery] string? search, [FromQuery] PagedQuery paging)
    {
        var query = _db.WorkflowDefinitions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(d => d.Category == category);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(d => d.Status == status);
        if (!string.IsNullOrWhiteSpace(changeClass)) query = query.Where(d => d.ChangeClass == changeClass);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(d => d.Code.Contains(s) || d.Name.Contains(s));
        }

        var totalCount = await query.CountAsync();
        var ordered = (paging.SortBy?.ToLowerInvariant()) switch
        {
            "code" => paging.Descending ? query.OrderByDescending(d => d.Code) : query.OrderBy(d => d.Code),
            "name" => paging.Descending ? query.OrderByDescending(d => d.Name) : query.OrderBy(d => d.Name),
            "status" => paging.Descending ? query.OrderByDescending(d => d.Status) : query.OrderBy(d => d.Status),
            _ => paging.Descending ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt)
        };

        var items = await ordered.ThenBy(d => d.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(d => new WorkflowDefinitionListDto
            {
                Id = d.Id, Code = d.Code, Name = d.Name, Category = d.Category,
                TriggerObjectType = d.TriggerObjectType, TriggerEvent = d.TriggerEvent, ChangeClass = d.ChangeClass,
                Status = d.Status, ActiveVersionId = d.ActiveVersionId,
                ActiveVersionNumber = d.Versions.Where(v => v.Id == d.ActiveVersionId).Select(v => (int?)v.VersionNumber).FirstOrDefault(),
                VersionCount = d.Versions.Count, IsSystem = d.IsSystem, CreatedAt = d.CreatedAt, UpdatedAt = d.UpdatedAt
            })
            .ToListAsync();

        return Ok(PagedResult<WorkflowDefinitionListDto>.Create(items, paging.Page, paging.PageSize, totalCount));
    }

    /// <summary>Tam workflow tanımı detayı (tüm sürümler, adımlar ve geçişler).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.WorkflowDefinitionRead)]
    public async Task<ActionResult<WorkflowDefinitionDetailDto>> GetById(Guid id)
    {
        var def = await LoadFull(id);
        if (def is null) return NotFound(new { message = "Workflow tanımı bulunamadı." });
        return Ok(MapDetail(def));
    }

    /// <summary>Yeni workflow tanımı ve ilk taslak sürüm oluşturur.</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.WorkflowDefinitionCreate)]
    public async Task<ActionResult<WorkflowDefinitionDetailDto>> Create([FromBody] CreateWorkflowDefinitionDto dto)
    {
        var created = await _service.CreateAsync(dto);
        var full = await LoadFull(created.Id);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapDetail(full!));
    }

    /// <summary>Taslak sürümün adım/geçiş grafiğini değiştirir (yayınlanmış sürümler değişmez).</summary>
    [HttpPut("versions/{versionId:guid}")]
    [Authorize(Policy = Permissions.WorkflowDefinitionUpdate)]
    public async Task<ActionResult<WorkflowVersionDto>> UpdateVersion(Guid versionId, [FromBody] UpdateWorkflowVersionDto dto)
    {
        var version = await _service.UpdateDraftVersionAsync(versionId, dto);
        return Ok(MapVersion(version));
    }

    /// <summary>Sürümü doğrular (yayın öncesi ön kontrol); değişiklik yapmaz.</summary>
    [HttpPost("versions/{versionId:guid}/validate")]
    [Authorize(Policy = Permissions.WorkflowDefinitionRead)]
    public async Task<ActionResult<WorkflowValidationResultDto>> Validate(Guid versionId)
        => Ok(await _service.ValidateVersionAsync(versionId));

    /// <summary>Sürümü doğrulayıp yayınlar (immutable).</summary>
    [HttpPost("versions/{versionId:guid}/publish")]
    [Authorize(Policy = Permissions.WorkflowDefinitionPublish)]
    public async Task<ActionResult<WorkflowVersionDto>> Publish(Guid versionId)
    {
        var version = await _service.PublishAsync(versionId);
        return Ok(MapVersion(version));
    }

    /// <summary>Yayınlanmış bir sürümü tanımın aktif sürümü yapar.</summary>
    [HttpPost("{id:guid}/versions/{versionId:guid}/activate")]
    [Authorize(Policy = Permissions.WorkflowDefinitionActivate)]
    public async Task<ActionResult<WorkflowDefinitionDetailDto>> Activate(Guid id, Guid versionId)
    {
        await _service.ActivateAsync(id, versionId);
        var full = await LoadFull(id);
        return Ok(MapDetail(full!));
    }

    /// <summary>Tanımın son sürümünü yeni bir taslak sürüme kopyalar.</summary>
    [HttpPost("{id:guid}/clone")]
    [Authorize(Policy = Permissions.WorkflowDefinitionCreate)]
    public async Task<ActionResult<WorkflowVersionDto>> Clone(Guid id)
    {
        var version = await _service.CloneLatestAsync(id);
        return Ok(MapVersion(version));
    }

    /// <summary>Workflow tanımını arşivler (sistem varsayılanları hariç).</summary>
    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = Permissions.WorkflowDefinitionArchive)]
    public async Task<ActionResult<WorkflowDefinitionDetailDto>> Archive(Guid id)
    {
        await _service.ArchiveAsync(id);
        var full = await LoadFull(id);
        return Ok(MapDetail(full!));
    }

    /* ── helpers ── */

    private Task<WorkflowDefinition?> LoadFull(Guid id) =>
        _db.WorkflowDefinitions
            .Include(d => d.Versions).ThenInclude(v => v.Steps)
            .Include(d => d.Versions).ThenInclude(v => v.Transitions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == id);

    private static WorkflowDefinitionDetailDto MapDetail(WorkflowDefinition d) => new()
    {
        Id = d.Id, Code = d.Code, Name = d.Name, Description = d.Description, Category = d.Category,
        TriggerObjectType = d.TriggerObjectType, TriggerEvent = d.TriggerEvent, ChangeClass = d.ChangeClass,
        Status = d.Status, ActiveVersionId = d.ActiveVersionId, IsSystem = d.IsSystem,
        CreatedAt = d.CreatedAt, UpdatedAt = d.UpdatedAt,
        RowVersion = d.RowVersion is { Length: > 0 } ? Convert.ToBase64String(d.RowVersion) : string.Empty,
        Versions = d.Versions.OrderBy(v => v.VersionNumber).Select(MapVersion).ToList()
    };

    private static WorkflowVersionDto MapVersion(WorkflowVersion v) => new()
    {
        Id = v.Id, VersionNumber = v.VersionNumber, Status = v.Status, StartStepKey = v.StartStepKey, Notes = v.Notes,
        CreatedAt = v.CreatedAt, PublishedAt = v.PublishedAt,
        RowVersion = v.RowVersion is { Length: > 0 } ? Convert.ToBase64String(v.RowVersion) : string.Empty,
        Steps = v.Steps.OrderBy(s => s.StepOrder).Select(s => new WorkflowStepDto
        {
            Id = s.Id, StepKey = s.StepKey, Name = s.Name, StepType = s.StepType, StepOrder = s.StepOrder,
            AssignedRole = s.AssignedRole, AssignedUserId = s.AssignedUserId, IsRequired = s.IsRequired,
            DueInHours = s.DueInHours, NotificationTemplateCode = s.NotificationTemplateCode,
            NotificationRole = s.NotificationRole, Description = s.Description
        }).ToList(),
        Transitions = v.Transitions.OrderBy(t => t.FromStepKey).ThenBy(t => t.Priority).Select(t => new WorkflowTransitionDto
        {
            Id = t.Id, FromStepKey = t.FromStepKey, ToStepKey = t.ToStepKey, ConditionType = t.ConditionType,
            Priority = t.Priority, ConditionField = t.ConditionField, Operator = t.Operator,
            ExpectedValue = t.ExpectedValue, Description = t.Description
        }).ToList()
    };
}
