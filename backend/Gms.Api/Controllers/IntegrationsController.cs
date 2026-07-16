using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Gms.Api.Services.Integrations;
using Gms.Api.Services.Integrations.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Controllers;

/// <summary>
/// Integration Hub management: definitions, credentials (secret-safe), endpoints, subscriptions,
/// connection tests, external links and audit. Thin controller — all rules live in
/// <see cref="IIntegrationService"/>. Secrets are never returned.
/// </summary>
[ApiController]
[Route("api/integrations")]
[Tags("Integrations")]
[Authorize]
public class IntegrationsController : ControllerBase
{
    private readonly GmsDbContext _db;
    private readonly IIntegrationService _service;
    private readonly IIntegrationProviderResolver _resolver;

    public IntegrationsController(GmsDbContext db, IIntegrationService service, IIntegrationProviderResolver resolver)
    {
        _db = db;
        _service = service;
        _resolver = resolver;
    }

    /// <summary>Filtrelenebilir + sayfalanabilir entegrasyon listesi.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.IntegrationRead)]
    public async Task<ActionResult<PagedResult<IntegrationListDto>>> GetAll(
        [FromQuery] string? provider, [FromQuery] string? status, [FromQuery] string? category,
        [FromQuery] string? search, [FromQuery] PagedQuery paging)
    {
        var query = _db.IntegrationDefinitions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(provider)) query = query.Where(d => d.Provider == provider);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(d => d.Status == status);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(d => d.Category == category);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(d => d.Code.Contains(s) || d.Name.Contains(s) || d.IntegrationNo.Contains(s));
        }

        var total = await query.CountAsync();
        var ordered = (paging.SortBy?.ToLowerInvariant()) switch
        {
            "code" => paging.Descending ? query.OrderByDescending(d => d.Code) : query.OrderBy(d => d.Code),
            "name" => paging.Descending ? query.OrderByDescending(d => d.Name) : query.OrderBy(d => d.Name),
            "status" => paging.Descending ? query.OrderByDescending(d => d.Status) : query.OrderBy(d => d.Status),
            _ => paging.Descending ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt)
        };
        var items = await ordered.ThenBy(d => d.Id).Skip(paging.Skip).Take(paging.PageSize)
            .Select(d => new IntegrationListDto
            {
                Id = d.Id, IntegrationNo = d.IntegrationNo, Code = d.Code, Name = d.Name, Provider = d.Provider,
                Category = d.Category, Status = d.Status, AuthenticationType = d.AuthenticationType, IsSystem = d.IsSystem,
                CreatedAt = d.CreatedAt, LastSuccessfulConnectionAt = d.LastSuccessfulConnectionAt, LastFailedConnectionAt = d.LastFailedConnectionAt
            }).ToListAsync();
        return Ok(PagedResult<IntegrationListDto>.Create(items, paging.Page, paging.PageSize, total));
    }

    /// <summary>Desteklenen sağlayıcı kataloğu (uygulanmış/yön bilgisiyle).</summary>
    [HttpGet("providers")]
    [Authorize(Policy = Permissions.IntegrationRead)]
    public ActionResult<IEnumerable<IntegrationProviderInfoDto>> GetProviders()
    {
        var list = IntegrationProviders.All.Select(p =>
        {
            var implemented = IntegrationProviders.Implemented.Contains(p);
            var info = new IntegrationProviderInfoDto { Provider = p, Implemented = implemented };
            if (implemented && _resolver.IsSupported(p))
            {
                var adapter = _resolver.Resolve(p);
                info.SupportsIncoming = adapter.SupportsIncoming;
                info.SupportsOutgoing = adapter.SupportsOutgoing;
            }
            return info;
        }).OrderByDescending(i => i.Implemented).ThenBy(i => i.Provider).ToList();
        return Ok(list);
    }

    /// <summary>Tüm dış nesne bağları (sayfalı).</summary>
    [HttpGet("links")]
    [Authorize(Policy = Permissions.IntegrationRead)]
    public async Task<ActionResult<PagedResult<ExternalObjectLinkDto>>> GetLinks(
        [FromQuery] string? internalObjectType, [FromQuery] Guid? integrationId, [FromQuery] PagedQuery paging)
    {
        var query = _db.ExternalObjectLinks.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(internalObjectType)) query = query.Where(l => l.InternalObjectType == internalObjectType);
        if (integrationId.HasValue) query = query.Where(l => l.IntegrationDefinitionId == integrationId.Value);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(l => l.CreatedAt).ThenBy(l => l.Id)
            .Skip(paging.Skip).Take(paging.PageSize).Select(MapLinkExpr).ToListAsync();
        return Ok(PagedResult<ExternalObjectLinkDto>.Create(items, paging.Page, paging.PageSize, total));
    }

    /// <summary>Bir dahili nesneye ait dış bağlar.</summary>
    [HttpGet("links/object/{objectType}/{objectId:guid}")]
    [Authorize(Policy = Permissions.IntegrationRead)]
    public async Task<ActionResult<IEnumerable<ExternalObjectLinkDto>>> GetLinksForObject(string objectType, Guid objectId)
    {
        var items = await _db.ExternalObjectLinks.AsNoTracking()
            .Where(l => l.InternalObjectType == objectType && l.InternalObjectId == objectId)
            .OrderByDescending(l => l.CreatedAt).Select(MapLinkExpr).ToListAsync();
        return Ok(items);
    }

    /// <summary>Tam entegrasyon detayı (kimlik bilgileri maskeli).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.IntegrationRead)]
    public async Task<ActionResult<IntegrationDetailDto>> GetById(Guid id)
    {
        var def = await LoadFull(id);
        if (def is null) return NotFound(new { message = "Entegrasyon bulunamadı." });
        return Ok(MapDetail(def));
    }

    /// <summary>Entegrasyon denetim olayları.</summary>
    [HttpGet("{id:guid}/audit")]
    [Authorize(Policy = Permissions.IntegrationAuditRead)]
    public async Task<ActionResult<IEnumerable<IntegrationEventDto>>> GetAudit(Guid id)
    {
        if (!await _db.IntegrationDefinitions.AnyAsync(d => d.Id == id)) return NotFound(new { message = "Entegrasyon bulunamadı." });
        var events = await _db.IntegrationEvents.AsNoTracking().Where(e => e.IntegrationDefinitionId == id)
            .OrderByDescending(e => e.CreatedAt).Select(MapEventExpr).ToListAsync();
        return Ok(events);
    }

    /// <summary>Entegrasyona ait yürütmeler (sayfalı).</summary>
    [HttpGet("{id:guid}/executions")]
    [Authorize(Policy = Permissions.IntegrationRead)]
    public async Task<ActionResult<PagedResult<IntegrationExecutionListDto>>> GetExecutions(Guid id, [FromQuery] PagedQuery paging)
    {
        var query = _db.IntegrationExecutions.AsNoTracking().Where(x => x.IntegrationDefinitionId == id);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
            .Skip(paging.Skip).Take(paging.PageSize).Select(MapExecListExpr).ToListAsync();
        return Ok(PagedResult<IntegrationExecutionListDto>.Create(items, paging.Page, paging.PageSize, total));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.IntegrationCreate)]
    public async Task<ActionResult<IntegrationDetailDto>> Create([FromBody] CreateIntegrationDto dto)
    {
        var created = await _service.CreateAsync(dto);
        var full = await LoadFull(created.Id);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapDetail(full!));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.IntegrationUpdate)]
    public async Task<ActionResult<IntegrationDetailDto>> Update(Guid id, [FromBody] UpdateIntegrationDto dto)
    {
        await _service.UpdateAsync(id, dto);
        var full = await LoadFull(id);
        return Ok(MapDetail(full!));
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = Permissions.IntegrationActivate)]
    public async Task<ActionResult<IntegrationDetailDto>> Activate(Guid id)
    {
        await _service.ActivateAsync(id);
        return Ok(MapDetail((await LoadFull(id))!));
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Permissions.IntegrationActivate)]
    public async Task<ActionResult<IntegrationDetailDto>> Deactivate(Guid id)
    {
        await _service.DeactivateAsync(id);
        return Ok(MapDetail((await LoadFull(id))!));
    }

    [HttpPost("{id:guid}/test-connection")]
    [Authorize(Policy = Permissions.IntegrationActivate)]
    [EnableRateLimiting("integration-sensitive")]
    public async Task<ActionResult<ConnectionTestResultDto>> TestConnection(Guid id)
        => Ok(await _service.TestConnectionAsync(id));

    /* ── credentials ── */

    [HttpPost("{id:guid}/credentials")]
    [Authorize(Policy = Permissions.IntegrationCredentialManage)]
    public async Task<ActionResult<IntegrationCredentialDto>> AddCredential(Guid id, [FromBody] CreateCredentialDto dto)
        => Ok(MapCredential(await _service.AddCredentialAsync(id, dto)));

    [HttpPut("{id:guid}/credentials/{credentialId:guid}")]
    [Authorize(Policy = Permissions.IntegrationCredentialManage)]
    public async Task<ActionResult<IntegrationCredentialDto>> RotateCredential(Guid id, Guid credentialId, [FromBody] UpdateCredentialDto dto)
        => Ok(MapCredential(await _service.RotateCredentialAsync(id, credentialId, dto)));

    [HttpDelete("{id:guid}/credentials/{credentialId:guid}")]
    [Authorize(Policy = Permissions.IntegrationCredentialManage)]
    public async Task<IActionResult> DeleteCredential(Guid id, Guid credentialId)
    {
        await _service.DeleteCredentialAsync(id, credentialId);
        return NoContent();
    }

    /* ── endpoints ── */

    [HttpPost("{id:guid}/endpoints")]
    [Authorize(Policy = Permissions.IntegrationEndpointManage)]
    public async Task<ActionResult<IntegrationEndpointDto>> AddEndpoint(Guid id, [FromBody] CreateEndpointDto dto)
        => Ok(MapEndpoint(await _service.AddEndpointAsync(id, dto)));

    [HttpPut("{id:guid}/endpoints/{endpointId:guid}")]
    [Authorize(Policy = Permissions.IntegrationEndpointManage)]
    public async Task<ActionResult<IntegrationEndpointDto>> UpdateEndpoint(Guid id, Guid endpointId, [FromBody] UpdateEndpointDto dto)
        => Ok(MapEndpoint(await _service.UpdateEndpointAsync(id, endpointId, dto)));

    [HttpDelete("{id:guid}/endpoints/{endpointId:guid}")]
    [Authorize(Policy = Permissions.IntegrationEndpointManage)]
    public async Task<IActionResult> DeleteEndpoint(Guid id, Guid endpointId)
    {
        await _service.DeleteEndpointAsync(id, endpointId);
        return NoContent();
    }

    /* ── subscriptions ── */

    [HttpPost("{id:guid}/subscriptions")]
    [Authorize(Policy = Permissions.IntegrationSubscriptionManage)]
    public async Task<ActionResult<IntegrationSubscriptionDto>> AddSubscription(Guid id, [FromBody] CreateSubscriptionDto dto)
        => Ok(MapSubscription(await _service.AddSubscriptionAsync(id, dto)));

    [HttpPut("{id:guid}/subscriptions/{subscriptionId:guid}")]
    [Authorize(Policy = Permissions.IntegrationSubscriptionManage)]
    public async Task<ActionResult<IntegrationSubscriptionDto>> UpdateSubscription(Guid id, Guid subscriptionId, [FromBody] UpdateSubscriptionDto dto)
        => Ok(MapSubscription(await _service.UpdateSubscriptionAsync(id, subscriptionId, dto)));

    [HttpDelete("{id:guid}/subscriptions/{subscriptionId:guid}")]
    [Authorize(Policy = Permissions.IntegrationSubscriptionManage)]
    public async Task<IActionResult> DeleteSubscription(Guid id, Guid subscriptionId)
    {
        await _service.DeleteSubscriptionAsync(id, subscriptionId);
        return NoContent();
    }

    /* ── external links ── */

    [HttpPost("{id:guid}/links")]
    [Authorize(Policy = Permissions.IntegrationLinkManage)]
    public async Task<ActionResult<ExternalObjectLinkDto>> CreateLink(Guid id, [FromBody] CreateExternalLinkDto dto)
    {
        var link = await _service.CreateLinkAsync(id, dto);
        var name = await _db.IntegrationDefinitions.Where(d => d.Id == id).Select(d => d.Name).FirstOrDefaultAsync() ?? string.Empty;
        return Ok(MapLink(link, name));
    }

    [HttpDelete("{id:guid}/links/{linkId:guid}")]
    [Authorize(Policy = Permissions.IntegrationLinkManage)]
    public async Task<IActionResult> RemoveLink(Guid id, Guid linkId)
    {
        await _service.RemoveLinkAsync(id, linkId);
        return NoContent();
    }

    /* ── mapping ── */

    private Task<IntegrationDefinition?> LoadFull(Guid id) =>
        _db.IntegrationDefinitions
            .Include(d => d.Credentials).Include(d => d.Endpoints).Include(d => d.Subscriptions)
            .AsSplitQuery().FirstOrDefaultAsync(d => d.Id == id);

    private static IntegrationDetailDto MapDetail(IntegrationDefinition d) => new()
    {
        Id = d.Id, IntegrationNo = d.IntegrationNo, Code = d.Code, Name = d.Name, Description = d.Description,
        Provider = d.Provider, Category = d.Category, Status = d.Status, BaseUrl = d.BaseUrl,
        AuthenticationType = d.AuthenticationType, IsSystem = d.IsSystem, CreatedAt = d.CreatedAt, UpdatedAt = d.UpdatedAt,
        LastSuccessfulConnectionAt = d.LastSuccessfulConnectionAt, LastFailedConnectionAt = d.LastFailedConnectionAt,
        RowVersion = d.RowVersion is { Length: > 0 } ? Convert.ToBase64String(d.RowVersion) : string.Empty,
        Credentials = d.Credentials.OrderBy(c => c.KeyName).Select(MapCredential).ToList(),
        Endpoints = d.Endpoints.OrderBy(e => e.CreatedAt).Select(MapEndpoint).ToList(),
        Subscriptions = d.Subscriptions.OrderBy(s => s.CreatedAt).Select(MapSubscription).ToList()
    };

    // Only metadata + mask — the encrypted/raw secret is never included.
    private static IntegrationCredentialDto MapCredential(IntegrationCredential c) => new()
    {
        Id = c.Id, CredentialType = c.CredentialType, KeyName = c.KeyName, MaskedValue = c.MaskedValue,
        CreatedAt = c.CreatedAt, RotatedAt = c.RotatedAt
    };

    private static IntegrationEndpointDto MapEndpoint(IntegrationEndpoint e) => new()
    {
        Id = e.Id, Name = e.Name, Direction = e.Direction, RelativePath = e.RelativePath, HttpMethod = e.HttpMethod,
        TimeoutSeconds = e.TimeoutSeconds, IsActive = e.IsActive, CreatedAt = e.CreatedAt
    };

    private static IntegrationSubscriptionDto MapSubscription(IntegrationSubscription s) => new()
    {
        Id = s.Id, EventType = s.EventType, ObjectType = s.ObjectType, TargetEndpointId = s.TargetEndpointId,
        IsActive = s.IsActive, CreatedAt = s.CreatedAt
    };

    private static ExternalObjectLinkDto MapLink(ExternalObjectLink l, string name) => new()
    {
        Id = l.Id, IntegrationDefinitionId = l.IntegrationDefinitionId, IntegrationName = name,
        InternalObjectType = l.InternalObjectType, InternalObjectId = l.InternalObjectId,
        ExternalObjectType = l.ExternalObjectType, ExternalObjectId = l.ExternalObjectId,
        ExternalObjectKey = l.ExternalObjectKey, ExternalUrl = l.ExternalUrl,
        CreatedByUserId = l.CreatedByUserId, CreatedAt = l.CreatedAt, LastSyncedAt = l.LastSyncedAt
    };

    private static readonly System.Linq.Expressions.Expression<Func<ExternalObjectLink, ExternalObjectLinkDto>> MapLinkExpr = l => new ExternalObjectLinkDto
    {
        Id = l.Id, IntegrationDefinitionId = l.IntegrationDefinitionId, IntegrationName = l.IntegrationDefinition!.Name,
        InternalObjectType = l.InternalObjectType, InternalObjectId = l.InternalObjectId,
        ExternalObjectType = l.ExternalObjectType, ExternalObjectId = l.ExternalObjectId,
        ExternalObjectKey = l.ExternalObjectKey, ExternalUrl = l.ExternalUrl,
        CreatedByUserId = l.CreatedByUserId, CreatedAt = l.CreatedAt, LastSyncedAt = l.LastSyncedAt
    };

    private static readonly System.Linq.Expressions.Expression<Func<IntegrationEvent, IntegrationEventDto>> MapEventExpr = e => new IntegrationEventDto
    {
        Id = e.Id, IntegrationExecutionId = e.IntegrationExecutionId, EventType = e.EventType,
        Description = e.Description, ActorUserId = e.ActorUserId, CreatedAt = e.CreatedAt
    };

    private static readonly System.Linq.Expressions.Expression<Func<IntegrationExecution, IntegrationExecutionListDto>> MapExecListExpr = x => new IntegrationExecutionListDto
    {
        Id = x.Id, ExecutionNo = x.ExecutionNo, IntegrationDefinitionId = x.IntegrationDefinitionId,
        IntegrationName = x.IntegrationDefinition!.Name, Direction = x.Direction, Operation = x.Operation,
        Status = x.Status, HttpStatusCode = x.HttpStatusCode, RetryCount = x.RetryCount, CorrelationId = x.CorrelationId,
        CreatedAt = x.CreatedAt, CompletedAt = x.CompletedAt
    };
}
