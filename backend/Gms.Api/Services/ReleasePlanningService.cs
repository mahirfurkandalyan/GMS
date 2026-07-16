using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Services;

public sealed record ReleaseCreateResult(ReleasePlan? Plan, string? Error);
public sealed record ReleaseActionResult(bool Ok, string? Error);

/// <summary>
/// Owns the release-planning lifecycle: validation (only approved, same scope,
/// no duplicates), number/risk/duration calculation, audit events, and the
/// Change status transitions driven by release lifecycle. Callers load/save.
/// </summary>
public class ReleasePlanningService
{
    private readonly GmsDbContext _db;
    private readonly SequentialNumberGenerator _numberGenerator;
    private readonly ReleaseRiskService _risk;
    private readonly Notifications.NotificationService _notifications;

    public ReleasePlanningService(GmsDbContext db, SequentialNumberGenerator numberGenerator, ReleaseRiskService risk,
        Notifications.NotificationService notifications)
    {
        _db = db;
        _numberGenerator = numberGenerator;
        _risk = risk;
        _notifications = notifications;
    }

    /// <summary>Validates and creates a release plan from approved changes. Does NOT save.</summary>
    public async Task<ReleaseCreateResult> CreateAsync(CreateReleasePlanDto dto, Guid actorUserId)
    {
        // --- Basic field validation ---
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Version))
            return new ReleaseCreateResult(null, "Yayın adı ve sürüm zorunludur.");
        if (!ReleaseTypes.All.Contains(dto.ReleaseType))
            return new ReleaseCreateResult(null, "Geçersiz yayın türü.");
        if (dto.ChangeIds is null || dto.ChangeIds.Count == 0)
            return new ReleaseCreateResult(null, "Yayın en az bir onaylı değişiklik içermelidir.");
        if (dto.ChangeIds.Distinct().Count() != dto.ChangeIds.Count)
            return new ReleaseCreateResult(null, "Aynı değişiklik birden fazla kez eklenemez.");
        if (!await _db.Users.AnyAsync(u => u.Id == dto.ReleaseManagerUserId))
            return new ReleaseCreateResult(null, "Yayın yöneticisi kullanıcı bulunamadı.");

        // --- Load & validate changes ---
        var changes = await _db.ChangeRequests
            .Include(c => c.Revisions)
            .Where(c => dto.ChangeIds.Contains(c.Id))
            .ToListAsync();

        if (changes.Count != dto.ChangeIds.Count)
            return new ReleaseCreateResult(null, "Seçilen değişikliklerden bazıları bulunamadı.");
        if (changes.Any(c => c.Status != ChangeStatuses.Approved))
            return new ReleaseCreateResult(null, "Yayın yalnızca 'Approved' durumundaki değişikliklerden oluşturulabilir.");

        var customerIds = changes.Select(c => c.CustomerId).Distinct().ToList();
        var projectIds = changes.Select(c => c.ProjectId).Distinct().ToList();
        var environmentIds = changes.Select(c => c.EnvironmentId).Distinct().ToList();
        if (customerIds.Count != 1) return new ReleaseCreateResult(null, "Tüm değişiklikler aynı müşteriye ait olmalıdır.");
        if (projectIds.Count != 1) return new ReleaseCreateResult(null, "Tüm değişiklikler aynı projeye ait olmalıdır.");
        if (environmentIds.Count != 1) return new ReleaseCreateResult(null, "Tüm değişiklikler aynı ortama ait olmalıdır.");

        // The release scope must match the changes' common scope.
        if (dto.CustomerId != customerIds[0]) return new ReleaseCreateResult(null, "Yayın müşterisi, değişikliklerin müşterisiyle eşleşmiyor.");
        if (dto.ProjectId != projectIds[0]) return new ReleaseCreateResult(null, "Yayın projesi, değişikliklerin projesiyle eşleşmiyor.");
        if (dto.EnvironmentId != environmentIds[0]) return new ReleaseCreateResult(null, "Yayın ortamı, değişikliklerin ortamıyla eşleşmiyor.");

        var now = DateTime.UtcNow;
        var releaseId = Guid.NewGuid();
        var releaseNo = await _numberGenerator.NextAsync($"REL-{now.Year}-", _db.ReleasePlans.Select(r => r.ReleaseNo));
        var environmentName = await _db.Environments.Where(e => e.Id == dto.EnvironmentId).Select(e => e.Name).FirstOrDefaultAsync() ?? string.Empty;

        var plan = new ReleasePlan
        {
            Id = releaseId,
            ReleaseNo = releaseNo,
            Name = dto.Name.Trim(),
            Version = dto.Version.Trim(),
            CustomerId = dto.CustomerId,
            ProjectId = dto.ProjectId,
            EnvironmentId = dto.EnvironmentId,
            ReleaseType = dto.ReleaseType,
            Status = ReleaseStatuses.Planned,
            PlannedDeploymentStart = dto.PlannedDeploymentStart,
            PlannedDeploymentEnd = dto.PlannedDeploymentEnd,
            RollbackWindow = dto.RollbackWindow?.Trim() ?? string.Empty,
            BusinessOwner = dto.BusinessOwner?.Trim() ?? string.Empty,
            TechnicalOwner = dto.TechnicalOwner?.Trim() ?? string.Empty,
            ReleaseManagerUserId = dto.ReleaseManagerUserId,
            Description = dto.Description?.Trim() ?? string.Empty,
            CreatedAt = now
        };

        // --- Items (ordered by the incoming ChangeIds order) ---
        var byId = changes.ToDictionary(c => c.Id);
        var order = 1;
        var totalMinutes = 0;
        foreach (var changeId in dto.ChangeIds)
        {
            var change = byId[changeId];
            var minutes = change.Revisions.Count > 0
                ? change.Revisions.OrderByDescending(r => r.RevisionNo).First().EstimatedDurationMinutes
                : 0;
            totalMinutes += minutes;

            plan.Items.Add(new ReleasePlanItem
            {
                Id = Guid.NewGuid(),
                ChangeRequestId = changeId,
                DeploymentOrder = order++,
                EstimatedMinutes = minutes,
                RollbackRequired = ChangeTypes.SqlRelated.Contains(change.ChangeType)
            });

            // Change integration: Approved → Scheduled.
            change.TransitionTo(ChangeStatuses.Scheduled);
            change.UpdatedAt = now;
            change.AuditEvents.Add(AuditFactory.Change(ChangeAuditEventTypes.ChangeScheduled, $"Değişiklik yayına planlandı: {releaseNo}.", actorUserId, now));
        }
        plan.TotalEstimatedMinutes = totalMinutes;

        // --- Deployment plan (1:1) ---
        var dp = dto.DeploymentPlan;
        plan.DeploymentPlan = new ReleaseDeploymentPlan
        {
            Id = Guid.NewGuid(),
            DeploymentStrategy = dp?.DeploymentStrategy?.Trim() ?? string.Empty,
            CommunicationPlan = dp?.CommunicationPlan?.Trim() ?? string.Empty,
            RollbackStrategy = dp?.RollbackStrategy?.Trim() ?? string.Empty,
            DowntimeExpected = dp?.DowntimeExpected ?? false,
            EstimatedDowntimeMinutes = dp?.EstimatedDowntimeMinutes ?? 0,
            Notes = dp?.Notes?.Trim() ?? string.Empty
        };

        // --- Documents ---
        foreach (var doc in dto.Documents ?? new List<CreateReleaseDocumentDto>())
        {
            plan.Documents.Add(new ReleaseDocument
            {
                Id = Guid.NewGuid(),
                DocumentType = doc.DocumentType, DocumentName = doc.DocumentName,
                Version = doc.Version ?? string.Empty, CreatedAt = now
            });
        }

        // --- Risk ---
        var riskInput = new ReleaseRiskInput(environmentName, changes.Select(c => new ReleaseRiskChangeInput(c.RiskLevel, c.ChangeClass)).ToList());
        var risk = _risk.Calculate(riskInput);
        plan.RiskScore = risk.Score;
        plan.RiskLevel = risk.Level;

        plan.AuditEvents.Add(AuditFactory.Release(ReleaseAuditEventTypes.ReleaseCreated, $"Yayın planı oluşturuldu ({releaseNo}) — {changes.Count} değişiklik, risk {risk.Level}.", actorUserId, now));

        _db.ReleasePlans.Add(plan);
        return new ReleaseCreateResult(plan, null);
    }

    public async Task<ReleaseActionResult> ScheduleAsync(ReleasePlan plan, Guid actorUserId)
    {
        if (plan.Status != ReleaseStatuses.Planned)
            return new ReleaseActionResult(false, "Yalnızca 'Planned' durumundaki yayın zamanlanabilir.");

        var now = DateTime.UtcNow;
        plan.TransitionTo(ReleaseStatuses.Scheduled);
        plan.UpdatedAt = now;
        plan.AuditEvents.Add(AuditFactory.Release(ReleaseAuditEventTypes.ReleaseScheduled, "Yayın zamanlandı.", actorUserId, now));

        // Notify release managers + executors that a release is scheduled (central engine).
        var data = new Dictionary<string, string> { ["ReleaseNo"] = plan.ReleaseNo, ["Name"] = plan.Name };
        await _notifications.NotifyRoleAsync(SystemRoles.ReleaseManager, Common.NotificationTemplates.ReleaseScheduled, Common.NotificationSeverities.Information, data, actorUserId);
        await _notifications.NotifyRoleAsync(SystemRoles.Executor, Common.NotificationTemplates.ReleaseScheduled, Common.NotificationSeverities.Information, data, actorUserId);

        return new ReleaseActionResult(true, null);
    }

    public async Task<ReleaseActionResult> CompleteAsync(ReleasePlan plan, Guid actorUserId)
    {
        if (plan.Status != ReleaseStatuses.Scheduled)
            return new ReleaseActionResult(false, "Yalnızca 'Scheduled' durumundaki yayın tamamlanabilir.");

        var now = DateTime.UtcNow;
        plan.TransitionTo(ReleaseStatuses.Completed);
        plan.UpdatedAt = now;
        plan.AuditEvents.Add(AuditFactory.Release(ReleaseAuditEventTypes.ReleaseCompleted, "Yayın tamamlandı.", actorUserId, now));

        // Change integration: Scheduled → Implemented.
        var changes = await LoadItemChanges(plan);
        foreach (var change in changes.Where(c => c.Status == ChangeStatuses.Scheduled))
        {
            change.TransitionTo(ChangeStatuses.Implemented);
            change.UpdatedAt = now;
            change.AuditEvents.Add(AuditFactory.Change(ChangeAuditEventTypes.ChangeImplemented, $"Değişiklik yayınla uygulandı: {plan.ReleaseNo}.", actorUserId, now));
        }

        return new ReleaseActionResult(true, null);
    }

    public async Task<ReleaseActionResult> CancelAsync(ReleasePlan plan, Guid actorUserId)
    {
        if (plan.Status == ReleaseStatuses.Completed)
            return new ReleaseActionResult(false, "Tamamlanmış yayın iptal edilemez.");
        if (plan.Status == ReleaseStatuses.Cancelled)
            return new ReleaseActionResult(false, "Yayın zaten iptal edilmiş.");

        var now = DateTime.UtcNow;
        plan.TransitionTo(ReleaseStatuses.Cancelled);
        plan.UpdatedAt = now;
        plan.AuditEvents.Add(AuditFactory.Release(ReleaseAuditEventTypes.ReleaseCancelled, "Yayın iptal edildi.", actorUserId, now));

        // Change integration: Scheduled → Approved (return to the pool).
        var changes = await LoadItemChanges(plan);
        foreach (var change in changes.Where(c => c.Status == ChangeStatuses.Scheduled))
        {
            change.TransitionTo(ChangeStatuses.Approved);
            change.UpdatedAt = now;
            change.AuditEvents.Add(AuditFactory.Change(ChangeAuditEventTypes.ChangeUnscheduled, $"Yayın iptal edildi; değişiklik onaylı havuza döndü: {plan.ReleaseNo}.", actorUserId, now));
        }

        return new ReleaseActionResult(true, null);
    }

    /* ── Private helpers ─────────────────────────────────── */

    private Task<List<ChangeRequest>> LoadItemChanges(ReleasePlan plan)
    {
        var ids = plan.Items.Select(i => i.ChangeRequestId).ToList();
        return _db.ChangeRequests.Include(c => c.AuditEvents).Where(c => ids.Contains(c.Id)).ToListAsync();
    }
}
