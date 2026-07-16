using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Services;

public sealed record ExecutionCreateResult(DeploymentRun? Run, string? Error);
public sealed record ExecutionActionResult(bool Ok, string? Error);

/// <summary>
/// Owns the deployment-execution lifecycle for a scheduled ReleasePlan: run/step
/// creation, ordered step advancement (single active step), success completion,
/// failure and rollback — plus the resulting Release and Change status transitions
/// and the full audit trail (DeploymentEvent + ReleaseAuditEvent + ChangeAuditEvent).
/// Execution extends Release Planning; it never re-implements release creation/validation.
/// Callers (the controller) load/save; this service mutates the tracked graph only.
/// </summary>
public class ExecutionService
{
    private readonly GmsDbContext _db;
    private readonly SequentialNumberGenerator _numbers;
    private readonly Notifications.NotificationService _notifications;

    public ExecutionService(GmsDbContext db, SequentialNumberGenerator numbers, Notifications.NotificationService notifications)
    {
        _db = db;
        _numbers = numbers;
        _notifications = notifications;
    }

    /// <summary>
    /// Creates a deployment run (status Created) with one Waiting step per release
    /// plan item, ordered by DeploymentOrder. Allowed only when the release is
    /// Scheduled and has no in-flight run. Does NOT save — the caller owns the txn.
    /// </summary>
    public async Task<ExecutionCreateResult> CreateAsync(CreateDeploymentRunDto dto, Guid actorUserId)
    {
        var release = await _db.ReleasePlans
            .Include(r => r.Items).ThenInclude(i => i.ChangeRequest)
            .FirstOrDefaultAsync(r => r.Id == dto.ReleasePlanId);

        if (release is null)
            return new ExecutionCreateResult(null, "Yayın planı bulunamadı.");
        if (release.Status != ReleaseStatuses.Scheduled)
            return new ExecutionCreateResult(null, "Yürütme yalnızca 'Scheduled' durumundaki yayın için oluşturulabilir.");
        if (release.Items.Count == 0)
            return new ExecutionCreateResult(null, "Yayın planında yürütülecek değişiklik bulunmuyor.");

        var hasActiveRun = await _db.DeploymentRuns.AnyAsync(r =>
            r.ReleasePlanId == release.Id &&
            (r.Status == DeploymentRunStatuses.Created || r.Status == DeploymentRunStatuses.Running));
        if (hasActiveRun)
            return new ExecutionCreateResult(null, "Bu yayın için hâlihazırda aktif bir yürütme mevcut.");

        var now = DateTime.UtcNow;
        var executionNo = await _numbers.NextAsync($"DEP-{now.Year}-", _db.DeploymentRuns.Select(r => r.ExecutionNo));

        var run = new DeploymentRun
        {
            Id = Guid.NewGuid(),
            ReleasePlanId = release.Id,
            ExecutionNo = executionNo,
            Status = DeploymentRunStatuses.Created,
            OverallResult = DeploymentResults.Pending,
            ExecutedByUserId = actorUserId,
            Notes = dto.Notes?.Trim() ?? string.Empty,
            CreatedAt = now
        };

        foreach (var item in release.Items.OrderBy(i => i.DeploymentOrder))
        {
            run.Steps.Add(new DeploymentStep
            {
                Id = Guid.NewGuid(),
                ReleasePlanItemId = item.Id,
                StepOrder = item.DeploymentOrder,
                Title = item.ChangeRequest is null
                    ? $"Adım {item.DeploymentOrder}"
                    : $"{item.ChangeRequest.ChangeNo} — {item.ChangeRequest.Title}",
                Status = DeploymentStepStatuses.Waiting,
                ExecutionResult = DeploymentResults.Pending,
                RollbackExecuted = false
            });
        }

        run.Events.Add(AuditFactory.Deployment(DeploymentEventTypes.ExecutionCreated,
            $"Yürütme oluşturuldu ({executionNo}) — {run.Steps.Count} adım.", actorUserId, now));

        _db.DeploymentRuns.Add(run);
        return new ExecutionCreateResult(run, null);
    }

    /// <summary>
    /// Starts the run: Created → Running, and the release Scheduled → InProgress.
    /// Steps are started individually via <see cref="StartNextStepAsync"/>.
    /// </summary>
    public async Task<ExecutionActionResult> StartAsync(DeploymentRun run, Guid actorUserId)
    {
        if (run.Status != DeploymentRunStatuses.Created)
            return new ExecutionActionResult(false, "Yalnızca 'Created' durumundaki yürütme başlatılabilir.");

        var release = await LoadRelease(run.ReleasePlanId);
        if (release is null)
            return new ExecutionActionResult(false, "İlişkili yayın planı bulunamadı.");
        if (release.Status != ReleaseStatuses.Scheduled)
            return new ExecutionActionResult(false, "Yürütme yalnızca 'Scheduled' durumundaki yayınla başlatılabilir.");

        var now = DateTime.UtcNow;
        run.TransitionTo(DeploymentRunStatuses.Running);
        run.StartedAt = now;
        run.ExecutedByUserId = actorUserId;
        run.Events.Add(AuditFactory.Deployment(DeploymentEventTypes.ExecutionStarted,
            "Yürütme başlatıldı.", actorUserId, now));

        release.TransitionTo(ReleaseStatuses.InProgress);
        release.UpdatedAt = now;
        release.AuditEvents.Add(AuditFactory.Release(ReleaseAuditEventTypes.ReleaseExecutionStarted,
            $"Yayın yürütmesi başladı ({run.ExecutionNo}).", actorUserId, now));

        // Notify executors that a deployment has started (central engine; does not save).
        await _notifications.NotifyRoleAsync(SystemRoles.Executor, Common.NotificationTemplates.ExecutionStarted,
            Common.NotificationSeverities.Information,
            new Dictionary<string, string> { ["ExecutionNo"] = run.ExecutionNo }, actorUserId);

        return new ExecutionActionResult(true, null);
    }

    /// <summary>
    /// Starts the next Waiting step (by StepOrder). Enforces "only one active step":
    /// rejected if a step is already Running.
    /// </summary>
    public ExecutionActionResult StartNextStep(DeploymentRun run, Guid actorUserId)
    {
        if (run.Status != DeploymentRunStatuses.Running)
            return new ExecutionActionResult(false, "Adım yalnızca 'Running' durumundaki yürütmede başlatılabilir.");
        if (run.Steps.Any(s => s.Status == DeploymentStepStatuses.Running))
            return new ExecutionActionResult(false, "Zaten aktif bir adım var; önce onu tamamlayın.");

        var next = run.Steps
            .Where(s => s.Status == DeploymentStepStatuses.Waiting)
            .OrderBy(s => s.StepOrder)
            .FirstOrDefault();
        if (next is null)
            return new ExecutionActionResult(false, "Başlatılacak bekleyen adım bulunmuyor.");

        var now = DateTime.UtcNow;
        next.Status = DeploymentStepStatuses.Running;
        next.StartedAt = now;
        next.ExecutedByUserId = actorUserId;
        run.Events.Add(AuditFactory.Deployment(DeploymentEventTypes.StepStarted,
            $"Adım {next.StepOrder} başlatıldı: {next.Title}.", actorUserId, now));

        return new ExecutionActionResult(true, null);
    }

    /// <summary>
    /// Completes the currently active step. If it was the last pending step, the run
    /// auto-completes (success): run → Completed, release → Completed, changes → Implemented.
    /// </summary>
    public async Task<ExecutionActionResult> CompleteStepAsync(DeploymentRun run, Guid actorUserId, string? notes)
    {
        if (run.Status != DeploymentRunStatuses.Running)
            return new ExecutionActionResult(false, "Adım yalnızca 'Running' durumundaki yürütmede tamamlanabilir.");

        var active = run.Steps.FirstOrDefault(s => s.Status == DeploymentStepStatuses.Running);
        if (active is null)
            return new ExecutionActionResult(false, "Tamamlanacak aktif adım bulunmuyor (önce adımı başlatın).");

        var now = DateTime.UtcNow;
        active.Status = DeploymentStepStatuses.Completed;
        active.CompletedAt = now;
        active.ExecutedByUserId = actorUserId;
        active.ExecutionResult = DeploymentResults.Success;
        if (!string.IsNullOrWhiteSpace(notes)) active.Notes = notes.Trim();
        run.Events.Add(AuditFactory.Deployment(DeploymentEventTypes.StepCompleted,
            $"Adım {active.StepOrder} tamamlandı: {active.Title}.", actorUserId, now));

        var pending = run.Steps.Any(s =>
            s.Status == DeploymentStepStatuses.Waiting || s.Status == DeploymentStepStatuses.Running);
        if (!pending)
            await CompleteExecutionAsync(run, actorUserId, now);

        return new ExecutionActionResult(true, null);
    }

    /// <summary>
    /// Fails the currently active step: step → Failed, run → Failed. The release stays
    /// InProgress (rollback may begin). No change status change.
    /// </summary>
    public async Task<ExecutionActionResult> FailStepAsync(DeploymentRun run, Guid actorUserId, string? reason)
    {
        if (run.Status != DeploymentRunStatuses.Running)
            return new ExecutionActionResult(false, "Adım yalnızca 'Running' durumundaki yürütmede başarısız işaretlenebilir.");

        var active = run.Steps.FirstOrDefault(s => s.Status == DeploymentStepStatuses.Running);
        if (active is null)
            return new ExecutionActionResult(false, "Başarısız işaretlenecek aktif adım bulunmuyor.");

        var now = DateTime.UtcNow;
        active.Status = DeploymentStepStatuses.Failed;
        active.CompletedAt = now;
        active.ExecutedByUserId = actorUserId;
        active.ExecutionResult = DeploymentResults.Failure;
        if (!string.IsNullOrWhiteSpace(reason)) active.Notes = reason.Trim();
        run.Events.Add(AuditFactory.Deployment(DeploymentEventTypes.StepFailed,
            $"Adım {active.StepOrder} başarısız: {active.Title}.", actorUserId, now));

        run.TransitionTo(DeploymentRunStatuses.Failed);
        run.OverallResult = DeploymentResults.Failure;
        run.Events.Add(AuditFactory.Deployment(DeploymentEventTypes.ExecutionFailed,
            "Yürütme başarısız oldu; geri alma (rollback) başlatılabilir.", actorUserId, now));
        // Release intentionally remains InProgress per the failure rules.

        // Notify release managers of the failure (central engine; does not save).
        await _notifications.NotifyRoleAsync(SystemRoles.ReleaseManager, Common.NotificationTemplates.ExecutionFailed,
            Common.NotificationSeverities.Error,
            new Dictionary<string, string> { ["ExecutionNo"] = run.ExecutionNo }, actorUserId);

        return new ExecutionActionResult(true, null);
    }

    /// <summary>
    /// Rolls back a failed run. Completed steps remain Completed; the failed step becomes
    /// RolledBack (RollbackExecuted); not-yet-run steps are Skipped. Run → RolledBack,
    /// release → Cancelled, affected changes → Approved (returned to the pool).
    /// </summary>
    public async Task<ExecutionActionResult> RollbackAsync(DeploymentRun run, Guid actorUserId, string? notes)
    {
        if (run.Status != DeploymentRunStatuses.Failed)
            return new ExecutionActionResult(false, "Yalnızca 'Failed' durumundaki yürütme geri alınabilir.");

        var now = DateTime.UtcNow;
        run.Events.Add(AuditFactory.Deployment(DeploymentEventTypes.RollbackStarted,
            "Geri alma başlatıldı.", actorUserId, now));

        foreach (var step in run.Steps)
        {
            switch (step.Status)
            {
                case DeploymentStepStatuses.Failed:
                    step.Status = DeploymentStepStatuses.RolledBack;
                    step.RollbackExecuted = true;
                    step.CompletedAt = now;
                    run.Events.Add(AuditFactory.Deployment(DeploymentEventTypes.StepRolledBack,
                        $"Adım {step.StepOrder} geri alındı: {step.Title}.", actorUserId, now));
                    break;
                case DeploymentStepStatuses.Completed:
                    // Completed steps remain completed; record that rollback ran over them.
                    step.RollbackExecuted = true;
                    break;
                case DeploymentStepStatuses.Waiting:
                    step.Status = DeploymentStepStatuses.Skipped;
                    step.CompletedAt = now;
                    run.Events.Add(AuditFactory.Deployment(DeploymentEventTypes.StepSkipped,
                        $"Adım {step.StepOrder} atlandı (çalıştırılmadı): {step.Title}.", actorUserId, now));
                    break;
            }
        }

        run.TransitionTo(DeploymentRunStatuses.RolledBack);
        run.OverallResult = DeploymentResults.RolledBack;
        run.CompletedAt = now;
        if (!string.IsNullOrWhiteSpace(notes)) run.Notes = notes.Trim();
        run.Events.Add(AuditFactory.Deployment(DeploymentEventTypes.ExecutionRolledBack,
            "Yürütme geri alındı.", actorUserId, now));

        var release = await LoadRelease(run.ReleasePlanId);
        if (release is not null)
        {
            release.TransitionTo(ReleaseStatuses.Cancelled);
            release.UpdatedAt = now;
            release.AuditEvents.Add(AuditFactory.Release(ReleaseAuditEventTypes.ReleaseCancelled,
                $"Yürütme geri alındığı için yayın iptal edildi ({run.ExecutionNo}).", actorUserId, now));

            foreach (var change in await LoadReleaseChanges(release))
            {
                if (change.Status == ChangeStatuses.Scheduled)
                {
                    change.TransitionTo(ChangeStatuses.Approved);
                    change.UpdatedAt = now;
                    change.AuditEvents.Add(AuditFactory.Change(ChangeAuditEventTypes.ChangeUnscheduled,
                        $"Yürütme geri alındı; değişiklik onaylı havuza döndü ({run.ExecutionNo}).", actorUserId, now));
                }
            }
        }

        return new ExecutionActionResult(true, null);
    }

    /* ── Private helpers ─────────────────────────────────── */

    /// <summary>Success path: run → Completed, release → Completed, changes → Implemented.</summary>
    private async Task CompleteExecutionAsync(DeploymentRun run, Guid actorUserId, DateTime now)
    {
        run.TransitionTo(DeploymentRunStatuses.Completed);
        run.OverallResult = DeploymentResults.Success;
        run.CompletedAt = now;
        run.Events.Add(AuditFactory.Deployment(DeploymentEventTypes.ExecutionCompleted,
            "Tüm adımlar tamamlandı; yürütme başarıyla tamamlandı.", actorUserId, now));

        var release = await LoadRelease(run.ReleasePlanId);
        if (release is null) return;

        release.TransitionTo(ReleaseStatuses.Completed);
        release.UpdatedAt = now;
        release.AuditEvents.Add(AuditFactory.Release(ReleaseAuditEventTypes.ReleaseCompleted,
            $"Yayın yürütmeyle tamamlandı ({run.ExecutionNo}).", actorUserId, now));

        // Notify the release manager that the deployment completed.
        await _notifications.NotifyUserAsync(release.ReleaseManagerUserId, Common.NotificationTemplates.ExecutionCompleted,
            Common.NotificationSeverities.Success,
            new Dictionary<string, string> { ["ExecutionNo"] = run.ExecutionNo }, actorUserId);

        foreach (var change in await LoadReleaseChanges(release))
        {
            if (change.Status == ChangeStatuses.Scheduled)
            {
                change.TransitionTo(ChangeStatuses.Implemented);
                change.UpdatedAt = now;
                change.AuditEvents.Add(AuditFactory.Change(ChangeAuditEventTypes.ChangeImplemented,
                    $"Değişiklik yürütmeyle uygulandı ({run.ExecutionNo}).", actorUserId, now));
            }
        }
    }

    private Task<ReleasePlan?> LoadRelease(Guid releasePlanId) =>
        _db.ReleasePlans.Include(r => r.Items).Include(r => r.AuditEvents)
            .FirstOrDefaultAsync(r => r.Id == releasePlanId);

    private Task<List<ChangeRequest>> LoadReleaseChanges(ReleasePlan release)
    {
        var changeIds = release.Items.Select(i => i.ChangeRequestId).ToList();
        return _db.ChangeRequests.Include(c => c.AuditEvents)
            .Where(c => changeIds.Contains(c.Id)).ToListAsync();
    }
}
