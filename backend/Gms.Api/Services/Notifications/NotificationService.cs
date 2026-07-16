using Gms.Api.Common;
using Gms.Api.Data;
using Gms.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Services.Notifications;

/// <summary>
/// The single, central notification engine for the whole platform. No business domain
/// creates notifications directly — they all call NotifyUser/NotifyRole/NotifyUsers/
/// NotifyBroadcast here. Notify* methods ADD to the tracked graph WITHOUT saving, so a
/// caller (controller/service) persists them atomically with its own transaction; the
/// standalone controller endpoints (broadcast/read/archive/preferences) then SaveChanges.
/// </summary>
public sealed class NotificationService
{
    private readonly GmsDbContext _db;
    private readonly SequentialNumberGenerator _numbers;
    private readonly ICurrentUser _currentUser;

    public NotificationService(GmsDbContext db, SequentialNumberGenerator numbers, ICurrentUser currentUser)
    {
        _db = db;
        _numbers = numbers;
        _currentUser = currentUser;
    }

    /// <summary>Notifies a single user from a template (does NOT save).</summary>
    public Task<Notification?> NotifyUserAsync(Guid userId, string templateCode, string severity,
        IReadOnlyDictionary<string, string>? data = null, Guid? createdBy = null, string? recipientRole = null, CancellationToken ct = default)
        => CreateFromTemplateAsync(userId, recipientRole, templateCode, severity, data, createdBy, ct);

    /// <summary>Notifies several users from a template (does NOT save).</summary>
    public async Task<int> NotifyUsersAsync(IEnumerable<Guid> userIds, string templateCode, string severity,
        IReadOnlyDictionary<string, string>? data = null, Guid? createdBy = null, CancellationToken ct = default)
    {
        var count = 0;
        foreach (var id in userIds.Distinct())
            if (await CreateFromTemplateAsync(id, null, templateCode, severity, data, createdBy, ct) is not null) count++;
        return count;
    }

    /// <summary>Fans a template out to every active user holding the given role (does NOT save).</summary>
    public async Task<int> NotifyRoleAsync(string role, string templateCode, string severity,
        IReadOnlyDictionary<string, string>? data = null, Guid? createdBy = null, CancellationToken ct = default)
    {
        var userIds = await _db.UserRoles
            .Where(ur => ur.Role!.Name == role && ur.AppUser!.IsActive)
            .Select(ur => ur.AppUserId).Distinct().ToListAsync(ct);

        var count = 0;
        foreach (var id in userIds)
            if (await CreateFromTemplateAsync(id, role, templateCode, severity, data, createdBy, ct) is not null) count++;
        return count;
    }

    /// <summary>Fans a template out to every active user (does NOT save).</summary>
    public async Task<int> NotifyBroadcastAsync(string templateCode, string severity,
        IReadOnlyDictionary<string, string>? data = null, Guid? createdBy = null, CancellationToken ct = default)
    {
        var userIds = await _db.Users.Where(u => u.IsActive).Select(u => u.Id).ToListAsync(ct);
        var count = 0;
        foreach (var id in userIds)
            if (await CreateFromTemplateAsync(id, "*", templateCode, severity, data, createdBy, ct) is not null) count++;
        return count;
    }

    /// <summary>
    /// Core factory: renders the template, applies the recipient's per-module preference,
    /// creates the Notification + channel deliveries (InApp always immediate; Email via the
    /// provider) + audit events. Returns null when the template is missing or the user has
    /// disabled every channel for that module. Does NOT save.
    /// </summary>
    public async Task<Notification?> CreateFromTemplateAsync(Guid recipientUserId, string? recipientRole, string templateCode,
        string severity, IReadOnlyDictionary<string, string>? data, Guid? createdBy, CancellationToken ct = default)
    {
        var template = await _db.NotificationTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Code == templateCode, ct);
        if (template is null) return null; // resilient: unknown template → skip

        return await CreateDirectAsync(recipientUserId, recipientRole,
            NotificationTemplates.Render(template.SubjectTemplate, data),
            NotificationTemplates.Render(template.BodyTemplate, data),
            templateCode, template.Module, severity, createdBy, ct);
    }

    /// <summary>Free-form broadcast (no template): fans a message out to all active users or a role. Does NOT save.</summary>
    public async Task<int> NotifyBroadcastDirectAsync(string title, string message, string severity, string? role, Guid? createdBy, CancellationToken ct = default)
    {
        var userIds = role is null
            ? await _db.Users.Where(u => u.IsActive).Select(u => u.Id).ToListAsync(ct)
            : await _db.UserRoles.Where(ur => ur.Role!.Name == role && ur.AppUser!.IsActive).Select(ur => ur.AppUserId).Distinct().ToListAsync(ct);

        var count = 0;
        foreach (var id in userIds)
            if (await CreateDirectAsync(id, role ?? "*", title, message, "SystemAnnouncement", NotificationModules.System, severity, createdBy, ct) is not null) count++;
        return count;
    }

    /// <summary>Lowest-level create: preference gate → notification + channel deliveries + audit. Does NOT save.</summary>
    public async Task<Notification?> CreateDirectAsync(Guid recipientUserId, string? recipientRole, string title, string message,
        string type, string module, string severity, Guid? createdBy, CancellationToken ct = default)
    {
        var (inApp, email) = await GetEffectivePreferenceAsync(recipientUserId, module, ct);
        if (!inApp && !email) return null; // recipient opted out of this module entirely

        var now = DateTime.UtcNow;
        var actor = createdBy ?? recipientUserId; // audit actor (system → self)
        var no = await NextNotificationNoAsync(now.Year, ct);

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            NotificationNo = no,
            Title = Truncate(title, 250),
            Message = Truncate(message, 2000),
            Type = type,
            Severity = NotificationSeverities.All.Contains(severity) ? severity : NotificationSeverities.Information,
            Module = module,
            Status = NotificationStatuses.Unread,
            RecipientUserId = recipientUserId,
            RecipientRole = recipientRole,
            CreatedByUserId = createdBy,
            CreatedAt = now
        };
        notification.Events.Add(AuditFactory.Notification(NotificationEventTypes.Created,
            $"Bildirim oluşturuldu ({no}) — {type}.", actor, now));

        if (inApp)
        {
            notification.Deliveries.Add(new NotificationDelivery
            {
                Id = Guid.NewGuid(), NotificationId = notification.Id,
                Channel = NotificationChannels.InApp, Status = NotificationDeliveryStatuses.Delivered, SentAt = now
            });
            notification.Events.Add(AuditFactory.Notification(NotificationEventTypes.InAppDelivered, "Uygulama içi iletildi.", actor, now));
        }

        if (email)
        {
            // Email is delivered OUT-OF-BAND by NotificationDeliveryWorker AFTER this transaction
            // commits — never a synchronous send inside a business transaction. Queue it Pending.
            notification.Deliveries.Add(new NotificationDelivery
            {
                Id = Guid.NewGuid(), NotificationId = notification.Id, Channel = NotificationChannels.Email,
                Status = NotificationDeliveryStatuses.Pending, AttemptCount = 0, RetryCount = 0
            });
            notification.Events.Add(AuditFactory.Notification(NotificationEventTypes.EmailQueued,
                "E-posta gönderimi kuyruğa alındı.", actor, now));
        }

        _db.Notifications.Add(notification);
        return notification;
    }

    /// <summary>Marks a notification Read (owner-only unless notification.manage). Does NOT save.</summary>
    public async Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        var n = await LoadOwnedAsync(notificationId, ct);
        if (n.Status == NotificationStatuses.Unread)
        {
            var now = DateTime.UtcNow;
            n.TransitionTo(NotificationStatuses.Read);
            n.ReadAt = now;
            n.Events.Add(AuditFactory.Notification(NotificationEventTypes.Read, "Okundu olarak işaretlendi.", _currentUser.RequireUserId(), now));
        }
    }

    /// <summary>Archives a notification (owner-only unless notification.manage). Does NOT save.</summary>
    public async Task ArchiveAsync(Guid notificationId, CancellationToken ct = default)
    {
        var n = await LoadOwnedAsync(notificationId, ct);
        var now = DateTime.UtcNow;
        n.TransitionTo(NotificationStatuses.Archived);
        n.Events.Add(AuditFactory.Notification(NotificationEventTypes.Archived, "Arşivlendi.", _currentUser.RequireUserId(), now));
    }

    /// <summary>Effective (UserId, Module) preference; defaults to both channels on when no row exists.</summary>
    public async Task<(bool InApp, bool Email)> GetEffectivePreferenceAsync(Guid userId, string module, CancellationToken ct = default)
    {
        var pref = await _db.NotificationPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Module == module, ct);
        return pref is null ? (true, true) : (pref.InAppEnabled, pref.EmailEnabled);
    }

    /// <summary>Upserts a user's per-module preference. Does NOT save.</summary>
    public async Task UpsertPreferenceAsync(Guid userId, string module, bool inApp, bool email, CancellationToken ct = default)
    {
        if (!NotificationModules.All.Contains(module))
            throw new AuthValidationException("Geçersiz bildirim modülü.");

        var now = DateTime.UtcNow;
        var pref = await _db.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId && p.Module == module, ct);
        if (pref is null)
        {
            _db.NotificationPreferences.Add(new NotificationPreference
            {
                Id = Guid.NewGuid(), UserId = userId, Module = module,
                InAppEnabled = inApp, EmailEnabled = email, CreatedAt = now
            });
        }
        else
        {
            pref.InAppEnabled = inApp;
            pref.EmailEnabled = email;
            pref.UpdatedAt = now;
        }
    }

    /* ── helpers ── */

    private async Task<Notification> LoadOwnedAsync(Guid id, CancellationToken ct)
    {
        var n = await _db.Notifications.Include(x => x.Events).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Bildirim bulunamadı.");
        var me = _currentUser.RequireUserId();
        if (n.RecipientUserId != me && !_currentUser.HasPermission(Permissions.NotificationManage))
            throw new AuthForbiddenException("Bu bildirim üzerinde yetkiniz yok.");
        return n;
    }

    /// <summary>Batch-safe number allocation: considers both saved and pending (tracked) rows.</summary>
    private async Task<string> NextNotificationNoAsync(int year, CancellationToken ct)
    {
        var prefix = $"NTF-{year}-";
        var maxSaved = await _db.Notifications.Where(n => n.NotificationNo.StartsWith(prefix))
            .Select(n => n.NotificationNo).OrderByDescending(x => x).FirstOrDefaultAsync(ct);
        var maxPending = _db.ChangeTracker.Entries<Notification>()
            .Where(e => e.State == EntityState.Added && e.Entity.NotificationNo.StartsWith(prefix))
            .Select(e => e.Entity.NotificationNo).OrderByDescending(x => x).FirstOrDefault();

        var max = new[] { maxSaved, maxPending }.Where(x => !string.IsNullOrEmpty(x)).DefaultIfEmpty(string.Empty).Max();
        var next = 1;
        if (!string.IsNullOrEmpty(max) && int.TryParse(max[prefix.Length..], out var parsed)) next = parsed + 1;
        return $"{prefix}{next:000000}";
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
