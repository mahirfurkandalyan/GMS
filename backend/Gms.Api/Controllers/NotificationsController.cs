using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Gms.Api.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Controllers;

/// <summary>
/// The single notification API for the platform. Thin controller — all notification
/// creation/delivery logic lives in <see cref="NotificationService"/>. List/read endpoints
/// are always scoped to the authenticated user's own notifications.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Tags("Notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly GmsDbContext _db;
    private readonly NotificationService _notifications;
    private readonly ICurrentUser _currentUser;

    public NotificationsController(GmsDbContext db, NotificationService notifications, ICurrentUser currentUser)
    {
        _db = db;
        _notifications = notifications;
        _currentUser = currentUser;
    }

    /// <summary>Kendi bildirimleri (sayfalı, filtreli). Silinenler varsayılan gizli.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.NotificationRead)]
    public Task<ActionResult<PagedResult<NotificationListDto>>> GetAll(
        [FromQuery] string? status, [FromQuery] string? severity, [FromQuery] string? module,
        [FromQuery] PagedQuery paging)
        => List(status, severity, module, paging);

    /// <summary>Kendi okunmamış bildirimleri.</summary>
    [HttpGet("unread")]
    [Authorize(Policy = Permissions.NotificationRead)]
    public Task<ActionResult<PagedResult<NotificationListDto>>> GetUnread([FromQuery] PagedQuery paging)
        => List(NotificationStatuses.Unread, null, null, paging);

    /// <summary>Kendi arşivlenmiş bildirimleri.</summary>
    [HttpGet("archived")]
    [Authorize(Policy = Permissions.NotificationRead)]
    public Task<ActionResult<PagedResult<NotificationListDto>>> GetArchived([FromQuery] PagedQuery paging)
        => List(NotificationStatuses.Archived, null, null, paging);

    /// <summary>Tek bildirim detayı (yalnızca sahibi).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.NotificationRead)]
    public async Task<ActionResult<NotificationDetailDto>> GetById(Guid id)
    {
        var me = _currentUser.RequireUserId();
        var n = await _db.Notifications.AsNoTracking()
            .Include(x => x.Deliveries).Include(x => x.Events)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);
        if (n is null) return NotFound(new { message = "Bildirim bulunamadı." });
        if (n.RecipientUserId != me && !_currentUser.HasPermission(Permissions.NotificationManage))
            return Forbid();
        return Ok(MapDetail(n));
    }

    /// <summary>Bildirimi okundu işaretler.</summary>
    [HttpPost("{id:guid}/read")]
    [Authorize(Policy = Permissions.NotificationRead)]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        await _notifications.MarkAsReadAsync(id);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Bildirimi arşivler.</summary>
    [HttpPost("{id:guid}/archive")]
    [Authorize(Policy = Permissions.NotificationArchive)]
    public async Task<IActionResult> Archive(Guid id)
    {
        await _notifications.ArchiveAsync(id);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Tüm aktif kullanıcılara veya bir role toplu bildirim gönderir (admin).</summary>
    [HttpPost("broadcast")]
    [Authorize(Policy = Permissions.NotificationBroadcast)]
    public async Task<ActionResult<object>> Broadcast([FromBody] BroadcastNotificationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest(new { message = "Başlık ve mesaj zorunludur." });
        if (!string.IsNullOrWhiteSpace(dto.Role) && !SystemRoles.All.Contains(dto.Role))
            return BadRequest(new { message = "Geçersiz rol." });

        var count = await _notifications.NotifyBroadcastDirectAsync(dto.Title.Trim(), dto.Message.Trim(),
            dto.Severity, string.IsNullOrWhiteSpace(dto.Role) ? null : dto.Role, _currentUser.RequireUserId());
        await _db.SaveChangesAsync();
        return Ok(new { delivered = count });
    }

    /// <summary>Kendi bildirim tercihleri (modül başına, satır yoksa varsayılan açık).</summary>
    [HttpGet("preferences")]
    [Authorize(Policy = Permissions.NotificationPreference)]
    public async Task<ActionResult<IEnumerable<NotificationPreferenceDto>>> GetPreferences()
    {
        var me = _currentUser.RequireUserId();
        var rows = await _db.NotificationPreferences.AsNoTracking().Where(p => p.UserId == me).ToListAsync();
        var result = NotificationModules.All.OrderBy(m => m).Select(m =>
        {
            var row = rows.FirstOrDefault(r => r.Module == m);
            return new NotificationPreferenceDto { Module = m, InAppEnabled = row?.InAppEnabled ?? true, EmailEnabled = row?.EmailEnabled ?? true };
        });
        return Ok(result);
    }

    /// <summary>Kendi bildirim tercihlerini günceller.</summary>
    [HttpPut("preferences")]
    [Authorize(Policy = Permissions.NotificationPreference)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesDto dto)
    {
        var me = _currentUser.RequireUserId();
        foreach (var p in dto.Preferences)
            await _notifications.UpsertPreferenceAsync(me, p.Module, p.InAppEnabled, p.EmailEnabled);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Bildirim şablonlarını listeler.</summary>
    [HttpGet("templates")]
    [Authorize(Policy = Permissions.NotificationTemplateManage)]
    public async Task<ActionResult<IEnumerable<NotificationTemplateDto>>> GetTemplates()
    {
        var templates = await _db.NotificationTemplates.AsNoTracking().OrderBy(t => t.Module).ThenBy(t => t.Code)
            .Select(t => new NotificationTemplateDto
            {
                Id = t.Id, Code = t.Code, Name = t.Name, SubjectTemplate = t.SubjectTemplate,
                BodyTemplate = t.BodyTemplate, Module = t.Module, IsSystem = t.IsSystem
            }).ToListAsync();
        return Ok(templates);
    }

    /// <summary>Bir şablonu günceller (kod ile).</summary>
    [HttpPut("templates/{code}")]
    [Authorize(Policy = Permissions.NotificationTemplateManage)]
    public async Task<ActionResult<NotificationTemplateDto>> UpdateTemplate(string code, [FromBody] UpdateTemplateDto dto)
    {
        var t = await _db.NotificationTemplates.FirstOrDefaultAsync(x => x.Code == code);
        if (t is null) return NotFound(new { message = "Şablon bulunamadı." });

        if (!string.IsNullOrWhiteSpace(dto.Name)) t.Name = dto.Name.Trim();
        if (dto.SubjectTemplate is not null) t.SubjectTemplate = dto.SubjectTemplate.Trim();
        if (dto.BodyTemplate is not null) t.BodyTemplate = dto.BodyTemplate.Trim();
        await _db.SaveChangesAsync();

        return Ok(new NotificationTemplateDto
        {
            Id = t.Id, Code = t.Code, Name = t.Name, SubjectTemplate = t.SubjectTemplate,
            BodyTemplate = t.BodyTemplate, Module = t.Module, IsSystem = t.IsSystem
        });
    }

    /// <summary>Bir bildirimin denetim olaylarını döndürür (yalnızca sahibi).</summary>
    [HttpGet("{id:guid}/audit")]
    [Authorize(Policy = Permissions.NotificationRead)]
    public async Task<ActionResult<IEnumerable<NotificationEventDto>>> GetAudit(Guid id)
    {
        var me = _currentUser.RequireUserId();
        var owner = await _db.Notifications.Where(n => n.Id == id).Select(n => (Guid?)n.RecipientUserId).FirstOrDefaultAsync();
        if (owner is null) return NotFound(new { message = "Bildirim bulunamadı." });
        if (owner != me && !_currentUser.HasPermission(Permissions.NotificationManage)) return Forbid();

        var events = await _db.NotificationEvents.AsNoTracking().Where(e => e.NotificationId == id)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new NotificationEventDto { Id = e.Id, EventType = e.EventType, Description = e.Description, ActorUserId = e.ActorUserId, CreatedAt = e.CreatedAt })
            .ToListAsync();
        return Ok(events);
    }

    /* ── helpers ── */

    private async Task<ActionResult<PagedResult<NotificationListDto>>> List(string? status, string? severity, string? module, PagedQuery paging)
    {
        var me = _currentUser.RequireUserId();
        var query = _db.Notifications.AsNoTracking().Where(n => n.RecipientUserId == me);

        query = string.IsNullOrWhiteSpace(status)
            ? query.Where(n => n.Status != NotificationStatuses.Deleted)
            : query.Where(n => n.Status == status);
        if (!string.IsNullOrWhiteSpace(severity)) query = query.Where(n => n.Severity == severity);
        if (!string.IsNullOrWhiteSpace(module)) query = query.Where(n => n.Module == module);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(n => n.CreatedAt).ThenBy(n => n.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(n => new NotificationListDto
            {
                Id = n.Id, NotificationNo = n.NotificationNo, Title = n.Title, Message = n.Message,
                Type = n.Type, Severity = n.Severity, Module = n.Module, Status = n.Status,
                RecipientRole = n.RecipientRole, ReadAt = n.ReadAt, CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return Ok(PagedResult<NotificationListDto>.Create(items, paging.Page, paging.PageSize, totalCount));
    }

    private static NotificationDetailDto MapDetail(Notification n) => new()
    {
        Id = n.Id, NotificationNo = n.NotificationNo, Title = n.Title, Message = n.Message,
        Type = n.Type, Severity = n.Severity, Module = n.Module, Status = n.Status,
        RecipientUserId = n.RecipientUserId, RecipientRole = n.RecipientRole, CreatedByUserId = n.CreatedByUserId,
        ReadAt = n.ReadAt, CreatedAt = n.CreatedAt,
        RowVersion = n.RowVersion is { Length: > 0 } ? Convert.ToBase64String(n.RowVersion) : string.Empty,
        Deliveries = n.Deliveries.Select(d => new NotificationDeliveryDto
        {
            Channel = d.Channel, Status = d.Status, SentAt = d.SentAt, FailureReason = d.FailureReason, RetryCount = d.RetryCount
        }).ToList(),
        Events = n.Events.OrderByDescending(e => e.CreatedAt).Select(e => new NotificationEventDto
        {
            Id = e.Id, EventType = e.EventType, Description = e.Description, ActorUserId = e.ActorUserId, CreatedAt = e.CreatedAt
        }).ToList()
    };
}
