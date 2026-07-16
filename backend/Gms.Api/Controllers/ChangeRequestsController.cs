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
[Route("api/change-requests")]
[Tags("ChangeRequests")]
[Authorize]
public class ChangeRequestsController : ControllerBase
{
    private readonly GmsDbContext _db;
    private readonly ChangeRiskService _risk;
    private readonly ChangeReadinessService _readiness;
    private readonly SequentialNumberGenerator _numberGenerator;
    private readonly ApprovalService _approval;
    private readonly ICurrentUser _currentUser;
    private readonly Gms.Api.Services.Notifications.NotificationService _notifications;
    private readonly Gms.Api.Services.Workflow.WorkflowRuntimeService _workflow;
    private readonly Gms.Api.Services.Integrations.IIntegrationService _integration;

    public ChangeRequestsController(
        GmsDbContext db, ChangeRiskService risk, ChangeReadinessService readiness,
        SequentialNumberGenerator numberGenerator, ApprovalService approval, ICurrentUser currentUser,
        Gms.Api.Services.Notifications.NotificationService notifications,
        Gms.Api.Services.Workflow.WorkflowRuntimeService workflow,
        Gms.Api.Services.Integrations.IIntegrationService integration)
    {
        _db = db;
        _risk = risk;
        _readiness = readiness;
        _numberGenerator = numberGenerator;
        _approval = approval;
        _currentUser = currentUser;
        _notifications = notifications;
        _workflow = workflow;
        _integration = integration;
    }

    /// <summary>
    /// Değişikliği bir dış nesneye (Jira issue, Azure DevOps work item vb.) bağlar. Normalleştirilmiş
    /// ilişki ExternalObjectLink olarak tutulur; SourceSystem/SourceReference alanları görüntü/geriye
    /// dönük uyumluluk için korunur. Değişiklik oluştururken sessizce ağ çağrısı yapılmaz.
    /// </summary>
    [HttpPost("{id:guid}/external-links")]
    [Authorize(Policy = Permissions.IntegrationLinkManage)]
    public async Task<ActionResult<ExternalObjectLinkDto>> LinkExternal(Guid id, [FromBody] LinkChangeExternalDto dto)
    {
        if (!await _db.ChangeRequests.AnyAsync(c => c.Id == id))
            return NotFound(new { message = "Değişiklik bulunamadı." });

        var link = await _integration.CreateLinkAsync(dto.IntegrationId, new CreateExternalLinkDto
        {
            InternalObjectType = "ChangeRequest", InternalObjectId = id,
            ExternalObjectType = dto.ExternalObjectType, ExternalReference = dto.ExternalReference, ExternalUrl = dto.ExternalUrl
        });
        var name = await _db.IntegrationDefinitions.Where(d => d.Id == dto.IntegrationId).Select(d => d.Name).FirstOrDefaultAsync() ?? string.Empty;
        return Ok(new ExternalObjectLinkDto
        {
            Id = link.Id, IntegrationDefinitionId = link.IntegrationDefinitionId, IntegrationName = name,
            InternalObjectType = link.InternalObjectType, InternalObjectId = link.InternalObjectId,
            ExternalObjectType = link.ExternalObjectType, ExternalObjectId = link.ExternalObjectId,
            ExternalObjectKey = link.ExternalObjectKey, ExternalUrl = link.ExternalUrl,
            CreatedByUserId = link.CreatedByUserId, CreatedAt = link.CreatedAt, LastSyncedAt = link.LastSyncedAt
        });
    }

    /// <summary>Filtrelenebilir + sayfalanabilir değişiklik listesi (özet).</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.ChangeRead)]
    public async Task<ActionResult<PagedResult<ChangeRequestListDto>>> GetAll(
        [FromQuery] Guid? customerId, [FromQuery] Guid? projectId, [FromQuery] Guid? environmentId,
        [FromQuery] string? status, [FromQuery] string? changeClass, [FromQuery] string? changeType,
        [FromQuery] string? riskLevel, [FromQuery] string? search, [FromQuery] PagedQuery paging)
    {
        var query = _db.ChangeRequests.AsNoTracking().AsQueryable();

        if (customerId.HasValue) query = query.Where(c => c.CustomerId == customerId.Value);
        if (projectId.HasValue) query = query.Where(c => c.ProjectId == projectId.Value);
        if (environmentId.HasValue) query = query.Where(c => c.EnvironmentId == environmentId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);
        if (!string.IsNullOrWhiteSpace(changeClass)) query = query.Where(c => c.ChangeClass == changeClass);
        if (!string.IsNullOrWhiteSpace(changeType)) query = query.Where(c => c.ChangeType == changeType);
        if (!string.IsNullOrWhiteSpace(riskLevel)) query = query.Where(c => c.RiskLevel == riskLevel);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(c => c.ChangeNo.Contains(s) || c.Title.Contains(s) || c.BusinessReason.Contains(s));
        }

        var totalCount = await query.CountAsync();

        // Whitelisted sort fields; default createdAt. Secondary key (Id) keeps paging stable.
        var ordered = (paging.SortBy?.ToLowerInvariant()) switch
        {
            "changeno" => paging.Descending ? query.OrderByDescending(c => c.ChangeNo) : query.OrderBy(c => c.ChangeNo),
            "title" => paging.Descending ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title),
            "status" => paging.Descending ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
            "riskscore" => paging.Descending ? query.OrderByDescending(c => c.RiskScore) : query.OrderBy(c => c.RiskScore),
            _ => paging.Descending ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt)
        };

        var items = await ordered.ThenBy(c => c.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(c => new ChangeRequestListDto
            {
                Id = c.Id, ChangeNo = c.ChangeNo, Title = c.Title,
                CustomerName = c.Customer!.Name, ProjectName = c.Project!.Name, EnvironmentName = c.Environment!.Name,
                ChangeClass = c.ChangeClass, ChangeType = c.ChangeType, Priority = c.Priority, Status = c.Status,
                RiskLevel = c.RiskLevel, RiskScore = c.RiskScore,
                PlannedImplementationDate = c.PlannedImplementationDate,
                CreatedByUserName = c.CreatedByUser!.FullName, CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();

        return Ok(PagedResult<ChangeRequestListDto>.Create(items, paging.Page, paging.PageSize, totalCount));
    }

    /// <summary>Tam detay: genel bilgi, son revizyon, varlıklar, dokümanlar, denetim, hazırlık.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.ChangeRead)]
    public async Task<ActionResult<ChangeRequestDetailDto>> GetById(Guid id)
    {
        var change = await LoadFull(id);
        if (change is null) return NotFound(new { message = "Değişiklik bulunamadı." });
        return Ok(await ToDetailAsync(change));
    }

    /// <summary>Değişiklik denetim olaylarını döndürür.</summary>
    [HttpGet("{id:guid}/audit")]
    [Authorize(Policy = Permissions.ChangeRead)]
    public async Task<ActionResult<IEnumerable<ChangeAuditEventDto>>> GetAudit(Guid id)
    {
        var exists = await _db.ChangeRequests.AnyAsync(c => c.Id == id);
        if (!exists) return NotFound(new { message = "Değişiklik bulunamadı." });

        var events = await _db.ChangeAuditEvents.AsNoTracking()
            .Where(e => e.ChangeRequestId == id)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new ChangeAuditEventDto
            {
                Id = e.Id, EventType = e.EventType, Description = e.Description, ActorUserId = e.ActorUserId,
                ActorUserName = _db.Users.Where(u => u.Id == e.ActorUserId).Select(u => u.FullName).FirstOrDefault() ?? string.Empty,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Yeni değişiklik oluşturur (durum: Draft), ilk revizyonla birlikte.</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.ChangeCreate)]
    public async Task<ActionResult<ChangeRequestDetailDto>> Create([FromBody] CreateChangeRequestDto dto)
    {
        var actor = _currentUser.RequireUserId();
        var validation = await ValidateCore(dto.Title, dto.ChangeClass, dto.ChangeType, dto.Priority,
            dto.CustomerId, dto.ProjectId, dto.EnvironmentId, actor);
        if (validation is not null) return validation;

        var now = DateTime.UtcNow;
        var changeId = Guid.NewGuid();
        var changeNo = await _numberGenerator.NextAsync($"CHG-{now.Year}-", _db.ChangeRequests.Select(c => c.ChangeNo));

        var change = new ChangeRequest
        {
            Id = changeId,
            ChangeNo = changeNo,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            BusinessReason = dto.BusinessReason?.Trim() ?? string.Empty,
            CustomerId = dto.CustomerId, ProjectId = dto.ProjectId, EnvironmentId = dto.EnvironmentId,
            ChangeClass = dto.ChangeClass, ChangeType = dto.ChangeType, Priority = dto.Priority,
            Status = ChangeStatuses.Draft,
            PlannedImplementationDate = dto.PlannedImplementationDate,
            PlannedRollbackDate = dto.PlannedRollbackDate,
            SourceSystem = dto.SourceSystem?.Trim(),
            SourceReference = dto.SourceReference?.Trim(),
            CreatedByUserId = actor,
            CreatedAt = now
        };

        // First revision (RevisionNo = 1)
        change.Revisions.Add(BuildRevision(dto.Revision, 1, now, actor));
        foreach (var a in dto.Assets) change.Assets.Add(BuildAsset(a));
        foreach (var d in dto.Documents) change.Documents.Add(BuildDocument(d, now));

        // Load the environment so risk calculation sees its name (PROD weight etc.).
        change.Environment = await _db.Environments.FirstOrDefaultAsync(e => e.Id == dto.EnvironmentId);
        RecalculateRisk(change);
        change.AuditEvents.Add(AuditFactory.Change(ChangeAuditEventTypes.ChangeCreated, "Değişiklik oluşturuldu.", actor, now));

        _db.ChangeRequests.Add(change);
        await _db.SaveChangesAsync();

        var full = await LoadFull(changeId);
        return CreatedAtAction(nameof(GetById), new { id = changeId }, await ToDetailAsync(full!));
    }

    /// <summary>Genel bilgileri günceller; riski yeniden hesaplar.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.ChangeUpdate)]
    public async Task<ActionResult<ChangeRequestDetailDto>> Update(Guid id, [FromBody] UpdateChangeRequestDto dto)
    {
        var actor = _currentUser.RequireUserId();
        var change = await LoadFull(id);
        if (change is null) return NotFound(new { message = "Değişiklik bulunamadı." });

        // Optimistic concurrency: if the client sent the token it loaded, enforce it.
        // A mismatch surfaces as DbUpdateConcurrencyException → 409 via middleware.
        if (!string.IsNullOrWhiteSpace(dto.RowVersion))
            _db.Entry(change).Property(c => c.RowVersion).OriginalValue = Convert.FromBase64String(dto.RowVersion);

        if (dto.ChangeClass is not null && !ChangeClasses.All.Contains(dto.ChangeClass))
            return BadRequest(new { message = "Geçersiz değişiklik sınıfı." });
        if (dto.ChangeType is not null && !ChangeTypes.All.Contains(dto.ChangeType))
            return BadRequest(new { message = "Geçersiz değişiklik türü." });
        if (dto.Priority is not null && !ChangePriorities.All.Contains(dto.Priority))
            return BadRequest(new { message = "Geçersiz öncelik." });

        if (dto.EnvironmentId.HasValue)
        {
            var env = await _db.Environments.FirstOrDefaultAsync(e => e.Id == dto.EnvironmentId.Value);
            if (env is null) return BadRequest(new { message = "Ortam bulunamadı." });
            if (env.ProjectId != change.ProjectId) return BadRequest(new { message = "Seçilen ortam bu projeye ait değil." });
            change.EnvironmentId = dto.EnvironmentId.Value;
            change.Environment = env;
        }

        if (!string.IsNullOrWhiteSpace(dto.Title)) change.Title = dto.Title.Trim();
        if (dto.Description is not null) change.Description = dto.Description.Trim();
        if (dto.BusinessReason is not null) change.BusinessReason = dto.BusinessReason.Trim();
        if (dto.ChangeClass is not null) change.ChangeClass = dto.ChangeClass;
        if (dto.ChangeType is not null) change.ChangeType = dto.ChangeType;
        if (dto.Priority is not null) change.Priority = dto.Priority;
        if (dto.PlannedImplementationDate.HasValue) change.PlannedImplementationDate = dto.PlannedImplementationDate;
        if (dto.PlannedRollbackDate.HasValue) change.PlannedRollbackDate = dto.PlannedRollbackDate;
        if (dto.SourceSystem is not null) change.SourceSystem = dto.SourceSystem.Trim();
        if (dto.SourceReference is not null) change.SourceReference = dto.SourceReference.Trim();

        var now = DateTime.UtcNow;
        change.UpdatedAt = now;
        RecalculateRisk(change);
        change.AuditEvents.Add(AuditFactory.Change(ChangeAuditEventTypes.ChangeUpdated, "Değişiklik güncellendi.", actor, now));

        await _db.SaveChangesAsync();
        return Ok(await ToDetailAsync(change));
    }

    /// <summary>Değişikliği incelemeye gönderir. Yalnızca Draft; kritik hazırlık bulgusu varsa 400.</summary>
    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = Permissions.ChangeSubmit)]
    public async Task<ActionResult<ChangeRequestDetailDto>> Submit(Guid id)
    {
        var change = await LoadFull(id);
        if (change is null) return NotFound(new { message = "Değişiklik bulunamadı." });

        if (change.Status != ChangeStatuses.Draft)
            return BadRequest(new { message = "Yalnızca taslak (Draft) durumundaki değişiklikler gönderilebilir." });

        var readiness = EvaluateReadiness(change);
        var criticalFindings = readiness.Findings.Where(f => f.Severity == ChangeReadinessService.SeverityCritical).ToList();
        if (criticalFindings.Count > 0)
        {
            return BadRequest(new
            {
                message = "Kritik hazırlık bulguları giderilmeden değişiklik gönderilemez.",
                readinessScore = readiness.ReadinessScore,
                findings = criticalFindings.Select(MapFinding)
            });
        }

        var now = DateTime.UtcNow;
        var actor = _currentUser.RequireUserId();

        // Strategy A — the Workflow Engine is the single orchestrator. Draft → Submitted, then the
        // governance workflow is started (which moves the change to UnderReview and assigns/notifies
        // the first task). The legacy ApprovalService is NOT invoked for new submissions; its
        // endpoints/records remain readable for history. Single transaction.
        change.TransitionTo(ChangeStatuses.Submitted);
        change.UpdatedAt = now;
        change.AuditEvents.Add(AuditFactory.Change(ChangeAuditEventTypes.ChangeSubmitted, "Değişiklik incelemeye gönderildi.", actor, now));

        await _workflow.StartForChangeAsync(change, readiness.ReadinessScore, actor);

        await _db.SaveChangesAsync();
        return Ok(await ToDetailAsync(change));
    }

    /// <summary>
    /// Değişikliği iptal eder. Geçerli olmayan geçişler (Implemented/Cancelled)
    /// TransitionTo tarafından 400 ile reddedilir. PART 3: iptal ile birlikte
    /// ilgili aktif onay talepleri de tek işlemde iptal edilir.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Permissions.ChangeCancel)]
    public async Task<ActionResult<ChangeRequestDetailDto>> Cancel(Guid id)
    {
        var change = await LoadFull(id);
        if (change is null) return NotFound(new { message = "Değişiklik bulunamadı." });

        var now = DateTime.UtcNow;
        var actor = _currentUser.RequireUserId();

        change.TransitionTo(ChangeStatuses.Cancelled);
        change.UpdatedAt = now;
        change.AuditEvents.Add(AuditFactory.Change(ChangeAuditEventTypes.ChangeCancelled, "Değişiklik iptal edildi.", actor, now));

        // Change/Approval consistency: cancel every active approval for this change.
        await _approval.CancelForChangeAsync(change.Id, actor, now);
        // Change/Workflow consistency: cancel every running workflow instance for this change.
        await _workflow.CancelForChangeAsync(change.Id, actor, now);

        await _db.SaveChangesAsync();
        return Ok(await ToDetailAsync(change));
    }

    /// <summary>Yeni revizyon oluşturur; RevisionNo artar, risk yeniden hesaplanır.</summary>
    [HttpPost("{id:guid}/revisions")]
    [Authorize(Policy = Permissions.ChangeRevisionCreate)]
    public async Task<ActionResult<ChangeRequestDetailDto>> AddRevision(Guid id, [FromBody] CreateChangeRevisionDto dto)
    {
        var actor = _currentUser.RequireUserId();
        var change = await LoadFull(id);
        if (change is null) return NotFound(new { message = "Değişiklik bulunamadı." });

        var now = DateTime.UtcNow;
        var nextRevisionNo = change.Revisions.Count == 0 ? 1 : change.Revisions.Max(r => r.RevisionNo) + 1;

        var revision = BuildRevision(dto, nextRevisionNo, now, actor);
        change.Revisions.Add(revision);
        change.UpdatedAt = now;
        RecalculateRisk(change);
        change.AuditEvents.Add(AuditFactory.Change(ChangeAuditEventTypes.RevisionCreated, $"Revizyon {nextRevisionNo} oluşturuldu.", actor, now));

        await _db.SaveChangesAsync();
        return Ok(await ToDetailAsync(change));
    }

    /* ── Private helpers ─────────────────────────────────── */

    private Task<ChangeRequest?> LoadFull(Guid id) =>
        _db.ChangeRequests
            .Include(c => c.Customer)
            .Include(c => c.Project)
            .Include(c => c.Environment)
            .Include(c => c.CreatedByUser)
            .Include(c => c.Revisions)
            .Include(c => c.Assets)
            .Include(c => c.Documents)
            .Include(c => c.AuditEvents)
            .AsSplitQuery() // multiple collection includes → avoid Cartesian explosion
            .FirstOrDefaultAsync(c => c.Id == id);

    private async Task<ActionResult?> ValidateCore(
        string title, string changeClass, string changeType, string priority,
        Guid customerId, Guid projectId, Guid environmentId, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(title)) return BadRequest(new { message = "Başlık zorunludur." });
        if (!ChangeClasses.All.Contains(changeClass)) return BadRequest(new { message = "Geçersiz değişiklik sınıfı." });
        if (!ChangeTypes.All.Contains(changeType)) return BadRequest(new { message = "Geçersiz değişiklik türü." });
        if (!ChangePriorities.All.Contains(priority)) return BadRequest(new { message = "Geçersiz öncelik." });

        if (!await _db.Customers.AnyAsync(c => c.Id == customerId)) return BadRequest(new { message = "Müşteri bulunamadı." });

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null) return BadRequest(new { message = "Proje bulunamadı." });
        if (project.CustomerId != customerId) return BadRequest(new { message = "Proje bu müşteriye ait değil." });

        var env = await _db.Environments.FirstOrDefaultAsync(e => e.Id == environmentId);
        if (env is null) return BadRequest(new { message = "Ortam bulunamadı." });
        if (env.ProjectId != projectId) return BadRequest(new { message = "Ortam bu projeye ait değil." });

        if (!await _db.Users.AnyAsync(u => u.Id == userId)) return BadRequest(new { message = "Oluşturan kullanıcı bulunamadı." });

        return null;
    }

    private static ChangeRevision LatestRevision(ChangeRequest c) =>
        c.Revisions.OrderByDescending(r => r.RevisionNo).First();

    private void RecalculateRisk(ChangeRequest c)
    {
        var latest = c.Revisions.Count > 0 ? LatestRevision(c) : null;
        var input = new ChangeRiskInput(
            EnvironmentName: c.Environment?.Name ?? string.Empty,
            ChangeClass: c.ChangeClass,
            ChangeType: c.ChangeType,
            HasCriticalAsset: c.Assets.Any(a => a.Criticality == ChangeRiskLevels.Critical),
            HasRollbackScript: !string.IsNullOrWhiteSpace(latest?.RollbackScript),
            HasBusinessReason: !string.IsNullOrWhiteSpace(c.BusinessReason));

        var result = _risk.Calculate(input);
        c.RiskScore = result.Score;
        c.RiskLevel = result.Level;
    }

    private ChangeReadinessResult EvaluateReadiness(ChangeRequest c)
    {
        var latest = c.Revisions.Count > 0 ? LatestRevision(c) : null;
        var input = new ChangeReadinessInput(
            HasBusinessReason: !string.IsNullOrWhiteSpace(c.BusinessReason),
            HasEnvironment: c.EnvironmentId != Guid.Empty,
            AssetCount: c.Assets.Count,
            ChangeType: c.ChangeType,
            HasRollbackScript: !string.IsNullOrWhiteSpace(latest?.RollbackScript),
            DocumentCount: c.Documents.Count,
            HasPlannedDate: c.PlannedImplementationDate.HasValue);

        return _readiness.Evaluate(input);
    }

    private static ChangeRevision BuildRevision(CreateChangeRevisionDto dto, int revisionNo, DateTime now, Guid createdBy) => new()
    {
        Id = Guid.NewGuid(), RevisionNo = revisionNo,
        TechnicalSummary = dto.TechnicalSummary ?? string.Empty,
        ImplementationNotes = dto.ImplementationNotes ?? string.Empty,
        DeploymentInstructions = dto.DeploymentInstructions ?? string.Empty,
        SqlScript = dto.SqlScript ?? string.Empty,
        RollbackScript = dto.RollbackScript ?? string.Empty,
        RollbackStrategy = dto.RollbackStrategy ?? string.Empty,
        RollbackOwner = dto.RollbackOwner ?? string.Empty,
        EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
        CreatedByUserId = createdBy, CreatedAt = now
    };

    private static ChangeAffectedAsset BuildAsset(CreateChangeAffectedAssetDto dto) => new()
    {
        Id = Guid.NewGuid(), AssetType = dto.AssetType, AssetName = dto.AssetName,
        Criticality = dto.Criticality, Description = dto.Description ?? string.Empty
    };

    private static ChangeDocument BuildDocument(CreateChangeDocumentDto dto, DateTime now) => new()
    {
        Id = Guid.NewGuid(), DocumentType = dto.DocumentType, DocumentName = dto.DocumentName,
        Version = dto.Version ?? string.Empty, Status = dto.Status ?? string.Empty, CreatedAt = now
    };

    /// <summary>Loads a full change and maps it to the detail DTO, resolving audit actor display names.</summary>
    private async Task<ChangeRequestDetailDto> ToDetailAsync(ChangeRequest c)
    {
        var actorIds = c.AuditEvents.Select(e => e.ActorUserId).Distinct().ToList();
        var names = await _db.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);
        return MapDetail(c, names);
    }

    private ChangeRequestDetailDto MapDetail(ChangeRequest c, IReadOnlyDictionary<Guid, string>? actorNames = null)
    {
        var latest = c.Revisions.Count > 0 ? LatestRevision(c) : null;
        var readiness = EvaluateReadiness(c);

        return new ChangeRequestDetailDto
        {
            Id = c.Id, ChangeNo = c.ChangeNo, Title = c.Title, Description = c.Description, BusinessReason = c.BusinessReason,
            CustomerId = c.CustomerId, CustomerName = c.Customer?.Name ?? string.Empty,
            ProjectId = c.ProjectId, ProjectName = c.Project?.Name ?? string.Empty,
            EnvironmentId = c.EnvironmentId, EnvironmentName = c.Environment?.Name ?? string.Empty,
            ChangeClass = c.ChangeClass, ChangeType = c.ChangeType, Priority = c.Priority, Status = c.Status,
            RiskLevel = c.RiskLevel, RiskScore = c.RiskScore,
            PlannedImplementationDate = c.PlannedImplementationDate, PlannedRollbackDate = c.PlannedRollbackDate,
            SourceSystem = c.SourceSystem, SourceReference = c.SourceReference,
            CreatedByUserId = c.CreatedByUserId, CreatedByUserName = c.CreatedByUser?.FullName ?? string.Empty,
            CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt,
            RowVersion = c.RowVersion is { Length: > 0 } ? Convert.ToBase64String(c.RowVersion) : string.Empty,
            LatestRevision = latest is null ? null : new ChangeRevisionDto
            {
                Id = latest.Id, RevisionNo = latest.RevisionNo, TechnicalSummary = latest.TechnicalSummary,
                ImplementationNotes = latest.ImplementationNotes, DeploymentInstructions = latest.DeploymentInstructions,
                SqlScript = latest.SqlScript, RollbackScript = latest.RollbackScript, RollbackStrategy = latest.RollbackStrategy,
                RollbackOwner = latest.RollbackOwner, EstimatedDurationMinutes = latest.EstimatedDurationMinutes,
                CreatedByUserId = latest.CreatedByUserId, CreatedAt = latest.CreatedAt
            },
            Assets = c.Assets.Select(a => new ChangeAffectedAssetDto
            {
                Id = a.Id, AssetType = a.AssetType, AssetName = a.AssetName, Criticality = a.Criticality, Description = a.Description
            }).ToList(),
            Documents = c.Documents.OrderBy(d => d.CreatedAt).Select(d => new ChangeDocumentDto
            {
                Id = d.Id, DocumentType = d.DocumentType, DocumentName = d.DocumentName, Version = d.Version, Status = d.Status, CreatedAt = d.CreatedAt
            }).ToList(),
            AuditEvents = c.AuditEvents.OrderByDescending(e => e.CreatedAt).Select(e => new ChangeAuditEventDto
            {
                Id = e.Id, EventType = e.EventType, Description = e.Description, ActorUserId = e.ActorUserId,
                ActorUserName = actorNames != null && actorNames.TryGetValue(e.ActorUserId, out var n) ? n : string.Empty,
                CreatedAt = e.CreatedAt
            }).ToList(),
            Readiness = new ChangeReadinessDto
            {
                ReadinessScore = readiness.ReadinessScore,
                Findings = readiness.Findings.Select(MapFinding).ToList()
            }
        };
    }

    private static ChangeReadinessFindingDto MapFinding(ChangeReadinessFinding f) => new()
    {
        Code = f.Code, Severity = f.Severity, Message = f.Message, Recommendation = f.Recommendation
    };
}
