using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Gms.Api.Services.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Controllers;

/// <summary>
/// Integration execution (outbox) runtime: browse executions/attempts, dispatch pending deliveries,
/// retry failed ones, and cancel. Thin controller over <see cref="IIntegrationDispatcher"/> and
/// <see cref="IIntegrationService"/>.
/// </summary>
[ApiController]
[Route("api/integration-executions")]
[Tags("IntegrationExecutions")]
[Authorize]
public class IntegrationExecutionsController : ControllerBase
{
    private readonly GmsDbContext _db;
    private readonly IIntegrationDispatcher _dispatcher;
    private readonly IIntegrationService _service;
    private readonly ICurrentUser _currentUser;

    public IntegrationExecutionsController(GmsDbContext db, IIntegrationDispatcher dispatcher,
        IIntegrationService service, ICurrentUser currentUser)
    {
        _db = db;
        _dispatcher = dispatcher;
        _service = service;
        _currentUser = currentUser;
    }

    /// <summary>Filtrelenebilir + sayfalanabilir yürütme listesi.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.IntegrationRead)]
    public async Task<ActionResult<PagedResult<IntegrationExecutionListDto>>> GetAll(
        [FromQuery] Guid? integrationId, [FromQuery] string? status, [FromQuery] string? direction,
        [FromQuery] string? correlationId, [FromQuery] PagedQuery paging)
    {
        var query = _db.IntegrationExecutions.AsNoTracking().AsQueryable();
        if (integrationId.HasValue) query = query.Where(x => x.IntegrationDefinitionId == integrationId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(direction)) query = query.Where(x => x.Direction == direction);
        if (!string.IsNullOrWhiteSpace(correlationId)) query = query.Where(x => x.CorrelationId == correlationId);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(x => new IntegrationExecutionListDto
            {
                Id = x.Id, ExecutionNo = x.ExecutionNo, IntegrationDefinitionId = x.IntegrationDefinitionId,
                IntegrationName = x.IntegrationDefinition!.Name, Direction = x.Direction, Operation = x.Operation,
                Status = x.Status, HttpStatusCode = x.HttpStatusCode, RetryCount = x.RetryCount,
                CorrelationId = x.CorrelationId, CreatedAt = x.CreatedAt, CompletedAt = x.CompletedAt
            }).ToListAsync();
        return Ok(PagedResult<IntegrationExecutionListDto>.Create(items, paging.Page, paging.PageSize, total));
    }

    /// <summary>Tam yürütme detayı (denemeler + olaylar).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.IntegrationRead)]
    public async Task<ActionResult<IntegrationExecutionDetailDto>> GetById(Guid id)
    {
        var x = await _db.IntegrationExecutions.AsNoTracking()
            .Include(e => e.IntegrationDefinition).Include(e => e.Attempts).Include(e => e.Events)
            .AsSplitQuery().FirstOrDefaultAsync(e => e.Id == id);
        if (x is null) return NotFound(new { message = "Yürütme bulunamadı." });
        return Ok(MapDetail(x));
    }

    /// <summary>Yürütme denetim olayları.</summary>
    [HttpGet("{id:guid}/audit")]
    [Authorize(Policy = Permissions.IntegrationAuditRead)]
    public async Task<ActionResult<IEnumerable<IntegrationEventDto>>> GetAudit(Guid id)
    {
        if (!await _db.IntegrationExecutions.AnyAsync(x => x.Id == id)) return NotFound(new { message = "Yürütme bulunamadı." });
        var events = await _db.IntegrationEvents.AsNoTracking().Where(e => e.IntegrationExecutionId == id)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new IntegrationEventDto
            {
                Id = e.Id, IntegrationExecutionId = e.IntegrationExecutionId, EventType = e.EventType,
                Description = e.Description, ActorUserId = e.ActorUserId, CreatedAt = e.CreatedAt
            }).ToListAsync();
        return Ok(events);
    }

    /// <summary>Başarısız bir yürütmeyi yeniden dener (tek geçiş).</summary>
    [HttpPost("{id:guid}/retry")]
    [Authorize(Policy = Permissions.IntegrationRetry)]
    [EnableRateLimiting("integration-sensitive")]
    public async Task<ActionResult<IntegrationExecutionDetailDto>> Retry(Guid id)
    {
        await _dispatcher.DispatchOneAsync(id, _currentUser.UserId);
        var x = await _db.IntegrationExecutions.AsNoTracking()
            .Include(e => e.IntegrationDefinition).Include(e => e.Attempts).Include(e => e.Events)
            .AsSplitQuery().FirstOrDefaultAsync(e => e.Id == id);
        return x is null ? NotFound(new { message = "Yürütme bulunamadı." }) : Ok(MapDetail(x));
    }

    /// <summary>Bekleyen/başarısız giden yürütmeleri gönderir (Admin tetikli dağıtım).</summary>
    [HttpPost("dispatch-pending")]
    [Authorize(Policy = Permissions.IntegrationExecute)]
    [EnableRateLimiting("integration-sensitive")]
    public async Task<ActionResult<DispatchResultDto>> DispatchPending([FromQuery] int max = 50)
        => Ok(await _dispatcher.DispatchPendingAsync(max, _currentUser.UserId));

    /// <summary>Bekleyen/başarısız bir yürütmeyi iptal eder.</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Permissions.IntegrationCancel)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _service.CancelExecutionAsync(id);
        return NoContent();
    }

    private static IntegrationExecutionDetailDto MapDetail(IntegrationExecution x) => new()
    {
        Id = x.Id, ExecutionNo = x.ExecutionNo, IntegrationDefinitionId = x.IntegrationDefinitionId,
        IntegrationName = x.IntegrationDefinition?.Name ?? string.Empty, Direction = x.Direction, Operation = x.Operation,
        ObjectType = x.ObjectType, ObjectId = x.ObjectId, CorrelationId = x.CorrelationId, Status = x.Status,
        StartedAt = x.StartedAt, CompletedAt = x.CompletedAt, RequestSummary = x.RequestSummary, ResponseSummary = x.ResponseSummary,
        HttpStatusCode = x.HttpStatusCode, ErrorCode = x.ErrorCode, ErrorMessage = x.ErrorMessage, RetryCount = x.RetryCount,
        CreatedAt = x.CreatedAt,
        RowVersion = x.RowVersion is { Length: > 0 } ? Convert.ToBase64String(x.RowVersion) : string.Empty,
        Attempts = x.Attempts.OrderBy(a => a.AttemptNo).Select(a => new IntegrationExecutionAttemptDto
        {
            Id = a.Id, AttemptNo = a.AttemptNo, StartedAt = a.StartedAt, CompletedAt = a.CompletedAt, Status = a.Status,
            HttpStatusCode = a.HttpStatusCode, ErrorMessage = a.ErrorMessage, DurationMilliseconds = a.DurationMilliseconds
        }).ToList(),
        Events = x.Events.OrderByDescending(e => e.CreatedAt).Select(e => new IntegrationEventDto
        {
            Id = e.Id, IntegrationExecutionId = e.IntegrationExecutionId, EventType = e.EventType,
            Description = e.Description, ActorUserId = e.ActorUserId, CreatedAt = e.CreatedAt
        }).ToList()
    };
}
