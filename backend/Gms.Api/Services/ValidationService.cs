using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Services;

public sealed record ValidationCreateResult(ValidationRun? Run, string? Error);
public sealed record ValidationActionResult(bool Ok, string? Error);

/// <summary>
/// Owns the validation lifecycle for a completed DeploymentRun: run/check creation,
/// ordered check advancement (single active check), pass/fail, and the resulting
/// ReleasePlan acceptance — plus the full audit trail (ValidationEvent + ReleaseAuditEvent).
/// Validation extends Execution; it never re-implements execution/release logic.
/// Callers (the controller) load/save; this service mutates the tracked graph only.
/// </summary>
public class ValidationService
{
    private readonly GmsDbContext _db;
    private readonly SequentialNumberGenerator _numbers;
    private readonly Notifications.NotificationService _notifications;

    public ValidationService(GmsDbContext db, SequentialNumberGenerator numbers, Notifications.NotificationService notifications)
    {
        _db = db;
        _numbers = numbers;
        _notifications = notifications;
    }

    /// <summary>
    /// Creates a validation run (status Created) with ordered Waiting checks. Allowed
    /// only when the deployment is Completed and has no in-flight validation. Checks come
    /// from the DTO, or one per release change if none supplied. Does NOT save.
    /// </summary>
    public async Task<ValidationCreateResult> CreateAsync(CreateValidationRunDto dto, Guid actorUserId)
    {
        var deployment = await _db.DeploymentRuns
            .Include(d => d.ReleasePlan).ThenInclude(r => r!.Items).ThenInclude(i => i.ChangeRequest)
            .FirstOrDefaultAsync(d => d.Id == dto.DeploymentRunId);

        if (deployment is null)
            return new ValidationCreateResult(null, "Yürütme (deployment) bulunamadı.");
        if (deployment.Status != DeploymentRunStatuses.Completed)
            return new ValidationCreateResult(null, "Doğrulama yalnızca 'Completed' durumundaki yürütme için başlatılabilir.");

        var validationType = string.IsNullOrWhiteSpace(dto.ValidationType) ? ValidationTypes.Functional : dto.ValidationType.Trim();
        if (!ValidationTypes.All.Contains(validationType))
            return new ValidationCreateResult(null, "Geçersiz doğrulama türü.");

        var hasActiveRun = await _db.ValidationRuns.AnyAsync(v =>
            v.DeploymentRunId == deployment.Id &&
            (v.Status == ValidationRunStatuses.Created || v.Status == ValidationRunStatuses.Running));
        if (hasActiveRun)
            return new ValidationCreateResult(null, "Bu yürütme için hâlihazırda aktif bir doğrulama mevcut.");

        var now = DateTime.UtcNow;
        var validationNo = await _numbers.NextAsync($"VAL-{now.Year}-", _db.ValidationRuns.Select(v => v.ValidationNo));

        var run = new ValidationRun
        {
            Id = Guid.NewGuid(),
            DeploymentRunId = deployment.Id,
            ValidationNo = validationNo,
            Status = ValidationRunStatuses.Created,
            ValidationType = validationType,
            OverallResult = ValidationResults.Pending,
            ValidatedByUserId = actorUserId,
            Summary = dto.Summary?.Trim() ?? string.Empty,
            CreatedAt = now
        };

        BuildChecks(run, dto, deployment.ReleasePlan);

        foreach (var ev in dto.Evidence ?? new List<CreateValidationEvidenceDto>())
        {
            run.Evidence.Add(new ValidationEvidence
            {
                Id = Guid.NewGuid(),
                EvidenceType = ev.EvidenceType, FileName = ev.FileName,
                Version = ev.Version ?? string.Empty, Description = ev.Description ?? string.Empty,
                CreatedAt = now
            });
        }

        run.Events.Add(AuditFactory.Validation(ValidationEventTypes.ValidationCreated,
            $"Doğrulama oluşturuldu ({validationNo}) — {run.Checks.Count} kontrol, tür {validationType}.", actorUserId, now));

        _db.ValidationRuns.Add(run);
        return new ValidationCreateResult(run, null);
    }

    /// <summary>Starts the validation: Created → Running. Release audit records the start.</summary>
    public async Task<ValidationActionResult> StartAsync(ValidationRun run, Guid actorUserId)
    {
        if (run.Status != ValidationRunStatuses.Created)
            return new ValidationActionResult(false, "Yalnızca 'Created' durumundaki doğrulama başlatılabilir.");

        var now = DateTime.UtcNow;
        run.TransitionTo(ValidationRunStatuses.Running);
        run.StartedAt = now;
        run.ValidatedByUserId = actorUserId;
        run.Events.Add(AuditFactory.Validation(ValidationEventTypes.ValidationStarted,
            "Doğrulama başlatıldı.", actorUserId, now));

        var release = await LoadReleaseForDeployment(run.DeploymentRunId);
        if (release is not null)
        {
            release.AuditEvents.Add(AuditFactory.Release(ReleaseAuditEventTypes.ReleaseValidationStarted,
                $"Yayın doğrulaması başladı ({run.ValidationNo}).", actorUserId, now));
        }

        return new ValidationActionResult(true, null);
    }

    /// <summary>
    /// Starts the next Waiting check (by CheckOrder). Enforces "only one active check":
    /// rejected if a check is already Running.
    /// </summary>
    public ValidationActionResult StartNextCheck(ValidationRun run, Guid actorUserId)
    {
        if (run.Status != ValidationRunStatuses.Running)
            return new ValidationActionResult(false, "Kontrol yalnızca 'Running' durumundaki doğrulamada başlatılabilir.");
        if (run.Checks.Any(c => c.Status == ValidationCheckStatuses.Running))
            return new ValidationActionResult(false, "Zaten aktif bir kontrol var; önce onu sonuçlandırın.");

        var next = run.Checks
            .Where(c => c.Status == ValidationCheckStatuses.Waiting)
            .OrderBy(c => c.CheckOrder)
            .FirstOrDefault();
        if (next is null)
            return new ValidationActionResult(false, "Başlatılacak bekleyen kontrol bulunmuyor.");

        var now = DateTime.UtcNow;
        next.Status = ValidationCheckStatuses.Running;
        next.ExecutedByUserId = actorUserId;
        run.Events.Add(AuditFactory.Validation(ValidationEventTypes.CheckStarted,
            $"Kontrol {next.CheckOrder} başlatıldı: {next.Title}.", actorUserId, now));

        return new ValidationActionResult(true, null);
    }

    /// <summary>
    /// Passes the active check. If it was the last check, the run auto-passes: run → Passed
    /// and the release → Accepted.
    /// </summary>
    public async Task<ValidationActionResult> PassCheckAsync(ValidationRun run, Guid actorUserId, string? actualResult, string? notes)
    {
        if (run.Status != ValidationRunStatuses.Running)
            return new ValidationActionResult(false, "Kontrol yalnızca 'Running' durumundaki doğrulamada sonuçlandırılabilir.");

        var active = run.Checks.FirstOrDefault(c => c.Status == ValidationCheckStatuses.Running);
        if (active is null)
            return new ValidationActionResult(false, "Sonuçlandırılacak aktif kontrol bulunmuyor (önce kontrolü başlatın).");

        var now = DateTime.UtcNow;
        active.Status = ValidationCheckStatuses.Passed;
        active.ExecutedAt = now;
        active.ExecutedByUserId = actorUserId;
        active.ActualResult = string.IsNullOrWhiteSpace(actualResult) ? active.ExpectedResult : actualResult.Trim();
        if (!string.IsNullOrWhiteSpace(notes)) active.Notes = notes.Trim();
        run.Events.Add(AuditFactory.Validation(ValidationEventTypes.CheckPassed,
            $"Kontrol {active.CheckOrder} geçti: {active.Title}.", actorUserId, now));

        var pending = run.Checks.Any(c =>
            c.Status == ValidationCheckStatuses.Waiting || c.Status == ValidationCheckStatuses.Running);
        if (!pending)
            await CompleteValidationPassedAsync(run, actorUserId, now);

        return new ValidationActionResult(true, null);
    }

    /// <summary>
    /// Fails the active check → the whole run fails (every check is mandatory). Remaining
    /// checks are Skipped. The release stays Completed (no automatic rollback — a business
    /// decision).
    /// </summary>
    public async Task<ValidationActionResult> FailCheckAsync(ValidationRun run, Guid actorUserId, string? actualResult, string? notes)
    {
        if (run.Status != ValidationRunStatuses.Running)
            return new ValidationActionResult(false, "Kontrol yalnızca 'Running' durumundaki doğrulamada sonuçlandırılabilir.");

        var active = run.Checks.FirstOrDefault(c => c.Status == ValidationCheckStatuses.Running);
        if (active is null)
            return new ValidationActionResult(false, "Sonuçlandırılacak aktif kontrol bulunmuyor.");

        var now = DateTime.UtcNow;
        active.Status = ValidationCheckStatuses.Failed;
        active.ExecutedAt = now;
        active.ExecutedByUserId = actorUserId;
        active.ActualResult = string.IsNullOrWhiteSpace(actualResult) ? "Beklenen sonuç alınamadı." : actualResult.Trim();
        if (!string.IsNullOrWhiteSpace(notes)) active.Notes = notes.Trim();
        run.Events.Add(AuditFactory.Validation(ValidationEventTypes.CheckFailed,
            $"Kontrol {active.CheckOrder} başarısız: {active.Title}.", actorUserId, now));

        // Not-yet-run checks never execute once the run fails.
        foreach (var waiting in run.Checks.Where(c => c.Status == ValidationCheckStatuses.Waiting))
        {
            waiting.Status = ValidationCheckStatuses.Skipped;
            run.Events.Add(AuditFactory.Validation(ValidationEventTypes.CheckSkipped,
                $"Kontrol {waiting.CheckOrder} atlandı (doğrulama başarısız): {waiting.Title}.", actorUserId, now));
        }

        run.TransitionTo(ValidationRunStatuses.Failed);
        run.OverallResult = ValidationResults.Failed;
        run.CompletedAt = now;
        run.Events.Add(AuditFactory.Validation(ValidationEventTypes.ValidationFailed,
            "Doğrulama başarısız oldu; yayın 'Completed' olarak kalır (otomatik rollback yok).", actorUserId, now));

        var release = await LoadReleaseForDeployment(run.DeploymentRunId);
        if (release is not null)
        {
            // Release remains Completed — no status change, only an audit trail entry.
            release.AuditEvents.Add(AuditFactory.Release(ReleaseAuditEventTypes.ReleaseValidationFailed,
                $"Yayın doğrulaması başarısız ({run.ValidationNo}); yayın Completed olarak korunuyor.", actorUserId, now));
        }

        // Notify QA that a validation failed (central engine; does not save).
        await _notifications.NotifyRoleAsync(SystemRoles.QA, Common.NotificationTemplates.ValidationFailed,
            Common.NotificationSeverities.Error,
            new Dictionary<string, string> { ["ValidationNo"] = run.ValidationNo }, actorUserId);

        return new ValidationActionResult(true, null);
    }

    /* ── Private helpers ─────────────────────────────────── */

    /// <summary>Pass path: run → Passed, release → Accepted (deployment unchanged).</summary>
    private async Task CompleteValidationPassedAsync(ValidationRun run, Guid actorUserId, DateTime now)
    {
        run.TransitionTo(ValidationRunStatuses.Passed);
        run.OverallResult = ValidationResults.Passed;
        run.CompletedAt = now;
        run.Events.Add(AuditFactory.Validation(ValidationEventTypes.ValidationPassed,
            "Tüm kontroller geçti; doğrulama başarıyla tamamlandı.", actorUserId, now));

        var release = await LoadReleaseForDeployment(run.DeploymentRunId);
        if (release is null) return;

        release.TransitionTo(ReleaseStatuses.Accepted);
        release.UpdatedAt = now;
        release.AuditEvents.Add(AuditFactory.Release(ReleaseAuditEventTypes.ReleaseAccepted,
            $"Yayın doğrulandı ve kabul edildi ({run.ValidationNo}).", actorUserId, now));

        // Notify the release manager that validation passed and the release is accepted.
        await _notifications.NotifyUserAsync(release.ReleaseManagerUserId, Common.NotificationTemplates.ValidationPassed,
            Common.NotificationSeverities.Success,
            new Dictionary<string, string> { ["ValidationNo"] = run.ValidationNo }, actorUserId);
    }

    /// <summary>
    /// Builds ordered Waiting checks: from the DTO when supplied, otherwise one check per
    /// release change (tying validation to what was deployed).
    /// </summary>
    private static void BuildChecks(ValidationRun run, CreateValidationRunDto dto, ReleasePlan? release)
    {
        var order = 1;
        if (dto.Checks is { Count: > 0 })
        {
            foreach (var c in dto.Checks)
            {
                run.Checks.Add(new ValidationCheck
                {
                    Id = Guid.NewGuid(),
                    CheckOrder = order++,
                    Title = string.IsNullOrWhiteSpace(c.Title) ? $"Kontrol {order - 1}" : c.Title.Trim(),
                    ExpectedResult = c.ExpectedResult?.Trim() ?? "Beklenen sonuç elde edildi.",
                    Status = ValidationCheckStatuses.Waiting
                });
            }
            return;
        }

        // Default: one check per release change, ordered by deployment order.
        var items = release?.Items.OrderBy(i => i.DeploymentOrder) ?? Enumerable.Empty<ReleasePlanItem>();
        foreach (var item in items)
        {
            var change = item.ChangeRequest;
            run.Checks.Add(new ValidationCheck
            {
                Id = Guid.NewGuid(),
                CheckOrder = order++,
                Title = change is null ? $"Doğrulama kontrolü {order - 1}" : $"Doğrulama: {change.ChangeNo} — {change.Title}",
                ExpectedResult = "Değişiklik üretimde beklendiği gibi çalışıyor.",
                Status = ValidationCheckStatuses.Waiting
            });
        }

        // Fallback so a run always has at least one check.
        if (run.Checks.Count == 0)
        {
            run.Checks.Add(new ValidationCheck
            {
                Id = Guid.NewGuid(), CheckOrder = 1, Title = "Genel doğrulama kontrolü",
                ExpectedResult = "Dağıtım beklendiği gibi çalışıyor.", Status = ValidationCheckStatuses.Waiting
            });
        }
    }

    private async Task<ReleasePlan?> LoadReleaseForDeployment(Guid deploymentRunId)
    {
        var releaseId = await _db.DeploymentRuns
            .Where(d => d.Id == deploymentRunId)
            .Select(d => d.ReleasePlanId)
            .FirstOrDefaultAsync();
        if (releaseId == Guid.Empty) return null;

        return await _db.ReleasePlans.Include(r => r.AuditEvents).FirstOrDefaultAsync(r => r.Id == releaseId);
    }
}
