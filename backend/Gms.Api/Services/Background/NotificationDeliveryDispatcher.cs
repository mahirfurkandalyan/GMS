using Gms.Api.Common;
using Gms.Api.Common.Observability;
using Gms.Api.Data;
using Gms.Api.Domain;
using Gms.Api.Services.Integrations;
using Gms.Api.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Services.Background;

/// <summary>
/// Claims and processes pending Email <see cref="NotificationDelivery"/> rows out-of-band (after
/// the notification transaction has committed). InApp deliveries remain immediate; Email is sent
/// here via <see cref="IEmailProvider"/>. Transient failures are retried with backoff; permanent
/// failures (<see cref="EmailPermanentException"/> / invalid recipient) dead-letter immediately.
/// A lease (LockedUntil/LockedBy) + RowVersion prevents duplicate sends across worker instances.
/// </summary>
public sealed class NotificationDeliveryDispatcher
{
    private readonly GmsDbContext _db;
    private readonly IEmailProvider _email;
    private readonly IIntegrationDelayStrategy _delay;

    public NotificationDeliveryDispatcher(GmsDbContext db, IEmailProvider email, IIntegrationDelayStrategy delay)
    {
        _db = db;
        _email = email;
        _delay = delay;
    }

    /// <summary>Atomically claims a batch of due Email deliveries for one worker (lease + RowVersion).</summary>
    public async Task<IReadOnlyList<Guid>> ClaimAsync(string owner, TimeSpan lease, int batch, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var candidates = await _db.NotificationDeliveries
            .Where(d => d.Channel == NotificationChannels.Email
                && (d.Status == NotificationDeliveryStatuses.Pending
                    || (d.Status == NotificationDeliveryStatuses.Failed && (d.NextAttemptAt == null || d.NextAttemptAt <= now)))
                && (d.LockedUntil == null || d.LockedUntil < now))
            .OrderBy(d => d.Notification!.CreatedAt).Take(batch)
            .ToListAsync(ct);

        var claimed = new List<Guid>();
        foreach (var d in candidates)
        {
            d.LockedUntil = now.Add(lease);
            d.LockedBy = owner;
            try { await _db.SaveChangesAsync(ct); claimed.Add(d.Id); }
            catch (DbUpdateConcurrencyException) { _db.Entry(d).State = EntityState.Detached; }
        }
        return claimed;
    }

    /// <summary>Sends one claimed delivery and persists the outcome. Returns the resulting status.</summary>
    public async Task<string> ProcessAsync(Guid deliveryId, int maxRetry, CancellationToken ct)
    {
        var d = await _db.NotificationDeliveries
            .Include(x => x.Notification).ThenInclude(n => n!.RecipientUser)
            .FirstOrDefaultAsync(x => x.Id == deliveryId, ct);
        if (d is null) return "NotFound";
        if (d.Status is NotificationDeliveryStatuses.Sent or NotificationDeliveryStatuses.Delivered or NotificationDeliveryStatuses.DeadLetter)
        {
            ClearLease(d); await _db.SaveChangesAsync(ct); return d.Status;
        }

        var now = DateTime.UtcNow;
        d.Status = NotificationDeliveryStatuses.Processing;
        d.AttemptCount++;
        d.LastAttemptAt = now;
        GmsTelemetry.NotificationDeliveries.Add(1, GmsTelemetry.Channel(NotificationChannels.Email));

        var user = d.Notification?.RecipientUser;
        var email = user?.Email ?? string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                throw new EmailPermanentException("Geçersiz alıcı e-posta adresi.");

            var ok = await _email.SendAsync(
                new EmailMessage(email, user!.FullName, d.Notification!.Title, d.Notification.Message), ct);
            if (ok)
            {
                d.Status = NotificationDeliveryStatuses.Sent;
                d.SentAt = DateTime.UtcNow;
                d.FailureReason = null;
                ClearLease(d);
                await _db.SaveChangesAsync(ct);
                return NotificationDeliveryStatuses.Sent;
            }
            return await FailAsync(d, "E-posta sağlayıcı gönderemedi.", transient: true, maxRetry, ct);
        }
        catch (EmailPermanentException ex)
        {
            return await FailAsync(d, ex.Message, transient: false, maxRetry, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return await FailAsync(d, ex.Message, transient: true, maxRetry, ct);
        }
    }

    private async Task<string> FailAsync(NotificationDelivery d, string reason, bool transient, int maxRetry, CancellationToken ct)
    {
        GmsTelemetry.NotificationDeliveryFailures.Add(1, GmsTelemetry.Channel(NotificationChannels.Email));
        d.RetryCount++;
        d.FailureReason = Truncate(reason, 500);

        if (!transient || d.AttemptCount >= maxRetry)
        {
            d.Status = NotificationDeliveryStatuses.DeadLetter;
            d.NextAttemptAt = null;
            ClearLease(d);
            await _db.SaveChangesAsync(ct);
            return NotificationDeliveryStatuses.DeadLetter;
        }

        d.Status = NotificationDeliveryStatuses.Failed;
        d.NextAttemptAt = DateTime.UtcNow.Add(_delay.NextDelay(d.AttemptCount));
        ClearLease(d);
        await _db.SaveChangesAsync(ct);
        return NotificationDeliveryStatuses.Failed;
    }

    private static void ClearLease(NotificationDelivery d) { d.LockedUntil = null; d.LockedBy = null; }
    private static string Truncate(string v, int max) => v.Length <= max ? v : v[..max];
}
