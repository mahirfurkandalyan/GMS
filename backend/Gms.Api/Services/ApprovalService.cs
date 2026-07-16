using Gms.Api.Common;
using Gms.Api.Data;
using Gms.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Services;

/// <summary>Result of an approval action (approve/reject/revision).</summary>
public sealed record ApprovalActionResult(bool Ok, string? Error);

/// <summary>
/// Owns the approval lifecycle: chain creation, step advancement, decisions,
/// audit events, and the resulting ChangeRequest status transitions. Callers
/// (controllers) load/save; this service mutates the tracked graph only.
/// </summary>
public class ApprovalService
{
    private readonly GmsDbContext _db;
    private readonly ApprovalChainService _chain;
    private readonly SequentialNumberGenerator _numberGenerator;
    private readonly ICurrentUser _currentUser;
    private readonly Notifications.NotificationService _notifications;

    public ApprovalService(GmsDbContext db, ApprovalChainService chain, SequentialNumberGenerator numberGenerator,
        ICurrentUser currentUser, Notifications.NotificationService notifications)
    {
        _db = db;
        _chain = chain;
        _numberGenerator = numberGenerator;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    /// <summary>Maps an approval step's ApproverRole to the fine-grained approve permission.</summary>
    private static string? ApprovePermissionFor(string approverRole) => approverRole switch
    {
        ApproverRoles.Architect => Permissions.ApprovalApproveArchitect,
        ApproverRoles.QA => Permissions.ApprovalApproveQa,
        ApproverRoles.ReleaseManager => Permissions.ApprovalApproveReleaseManager,
        ApproverRoles.Admin => Permissions.ApprovalApproveAdmin,
        _ => null
    };

    /// <summary>
    /// Creates an approval request + chain for a change and moves the change to
    /// UnderReview. Does NOT save — the caller (submit) owns the transaction.
    /// </summary>
    public async Task<ApprovalRequest> CreateForChangeAsync(ChangeRequest change, Guid requestedByUserId)
    {
        var now = DateTime.UtcNow;
        var approvalNo = await _numberGenerator.NextAsync($"APR-{now.Year}-", _db.ApprovalRequests.Select(a => a.ApprovalNo));

        var approval = new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            ApprovalNo = approvalNo,
            RelatedObjectType = ApprovalRelatedObjectTypes.ChangeRequest,
            RelatedObjectId = change.Id,
            Title = change.Title,
            Description = change.BusinessReason,
            Status = ApprovalStatuses.InProgress,
            Priority = change.Priority,
            RequestedByUserId = requestedByUserId,
            RequestedAt = now,
            CreatedAt = now
        };

        var definitions = _chain.BuildForChange(change.RiskLevel);
        foreach (var def in definitions)
        {
            approval.Steps.Add(new ApprovalStep
            {
                Id = Guid.NewGuid(),
                StepNo = def.StepNo,
                StepName = def.StepName,
                ApproverRole = def.ApproverRole,
                ApproverUserId = await ResolveApproverAsync(def.ApproverRole),
                Status = def.StepNo == 1 ? ApprovalStepStatuses.Active : ApprovalStepStatuses.Waiting,
                CreatedAt = now
            });
        }

        approval.AuditEvents.Add(AuditFactory.Approval(ApprovalEventTypes.ApprovalCreated, $"Onay talebi oluşturuldu ({approvalNo}).", requestedByUserId, now));
        var firstStep = approval.Steps.First(s => s.StepNo == 1);
        approval.AuditEvents.Add(AuditFactory.Approval(ApprovalEventTypes.StepActivated, $"Adım 1 aktifleştirildi: {firstStep.StepName}.", requestedByUserId, now));

        change.TransitionTo(ChangeStatuses.UnderReview);
        change.UpdatedAt = now;
        change.AuditEvents.Add(AuditFactory.Change(ChangeAuditEventTypes.ApprovalRequested, $"Onay talebi oluşturuldu: {approvalNo}.", requestedByUserId, now));

        _db.ApprovalRequests.Add(approval);

        // Notify the assigned first-step approver (central engine; does not save).
        if (firstStep.ApproverUserId is { } approverId)
            await _notifications.NotifyUserAsync(approverId, Common.NotificationTemplates.ApprovalRequired,
                Common.NotificationSeverities.Warning,
                new Dictionary<string, string> { ["ApprovalNo"] = approvalNo, ["StepName"] = firstStep.StepName }, requestedByUserId);

        return approval;
    }

    public async Task<ApprovalActionResult> ApproveAsync(ApprovalRequest approval, string comment, string signatureMeaning)
    {
        var actor = _currentUser.RequireUserId();
        var guard = GuardActiveStep(approval, actor, requireComment: false, comment, requireApprovePermission: true);
        if (guard.Error is not null) return new ApprovalActionResult(false, guard.Error);
        var active = guard.Step!;
        var now = DateTime.UtcNow;

        active.Status = ApprovalStepStatuses.Approved;
        active.CompletedAt = now;
        approval.Decisions.Add(BuildDecision(active.Id, ApprovalDecisions.Approved, comment, signatureMeaning, actor, now));
        approval.AuditEvents.Add(AuditFactory.Approval(ApprovalEventTypes.Approved, $"{active.StepName} onaylandı.", actor, now));
        approval.UpdatedAt = now;

        var next = approval.Steps.FirstOrDefault(s => s.StepNo == active.StepNo + 1 && s.Status == ApprovalStepStatuses.Waiting);
        if (next is not null)
        {
            next.Status = ApprovalStepStatuses.Active;
            approval.AuditEvents.Add(AuditFactory.Approval(ApprovalEventTypes.StepActivated, $"Adım {next.StepNo} aktifleştirildi: {next.StepName}.", actor, now));

            // Notify the next approver in the chain.
            if (next.ApproverUserId is { } nextApproverId)
                await _notifications.NotifyUserAsync(nextApproverId, Common.NotificationTemplates.ApprovalRequired,
                    Common.NotificationSeverities.Warning,
                    new Dictionary<string, string> { ["ApprovalNo"] = approval.ApprovalNo, ["StepName"] = next.StepName }, actor);
        }
        else
        {
            approval.TransitionTo(ApprovalStatuses.Approved);
            approval.CompletedAt = now;
            approval.AuditEvents.Add(AuditFactory.Approval(ApprovalEventTypes.ApprovalCompleted, "Tüm adımlar onaylandı; onay tamamlandı.", actor, now));

            var change = await LoadChange(approval.RelatedObjectId);
            if (change is not null)
            {
                change.TransitionTo(ChangeStatuses.Approved);
                change.UpdatedAt = now;
                change.AuditEvents.Add(AuditFactory.Change(ChangeAuditEventTypes.ChangeApproved, $"Değişiklik onaylandı ({approval.ApprovalNo}).", actor, now));

                // Notify the requester that their change was fully approved.
                await _notifications.NotifyUserAsync(change.CreatedByUserId, Common.NotificationTemplates.ApprovalApproved,
                    Common.NotificationSeverities.Success,
                    new Dictionary<string, string> { ["ChangeNo"] = change.ChangeNo }, actor);
            }
        }

        return new ApprovalActionResult(true, null);
    }

    public async Task<ApprovalActionResult> RejectAsync(ApprovalRequest approval, string comment, string signatureMeaning)
    {
        var actor = _currentUser.RequireUserId();
        var guard = GuardActiveStep(approval, actor, requireComment: true, comment, requireApprovePermission: false);
        if (guard.Error is not null) return new ApprovalActionResult(false, guard.Error);
        var active = guard.Step!;
        var now = DateTime.UtcNow;

        active.Status = ApprovalStepStatuses.Rejected;
        active.CompletedAt = now;
        approval.Decisions.Add(BuildDecision(active.Id, ApprovalDecisions.Rejected, comment, signatureMeaning, actor, now));
        approval.AuditEvents.Add(AuditFactory.Approval(ApprovalEventTypes.Rejected, $"{active.StepName} reddedildi.", actor, now));
        approval.TransitionTo(ApprovalStatuses.Rejected);
        approval.CompletedAt = now;
        approval.UpdatedAt = now;

        var change = await LoadChange(approval.RelatedObjectId);
        if (change is not null)
        {
            change.TransitionTo(ChangeStatuses.Submitted);
            change.UpdatedAt = now;
            change.AuditEvents.Add(AuditFactory.Change(ChangeAuditEventTypes.ChangeApprovalRejected, $"Onay reddedildi ({approval.ApprovalNo}).", actor, now));

            // Notify the requester that their change was rejected.
            await _notifications.NotifyUserAsync(change.CreatedByUserId, Common.NotificationTemplates.ApprovalRejected,
                Common.NotificationSeverities.Error,
                new Dictionary<string, string> { ["ChangeNo"] = change.ChangeNo }, actor);
        }

        return new ApprovalActionResult(true, null);
    }

    public async Task<ApprovalActionResult> RequestRevisionAsync(ApprovalRequest approval, string comment, string signatureMeaning)
    {
        var actor = _currentUser.RequireUserId();
        var guard = GuardActiveStep(approval, actor, requireComment: true, comment, requireApprovePermission: false);
        if (guard.Error is not null) return new ApprovalActionResult(false, guard.Error);
        var active = guard.Step!;
        var now = DateTime.UtcNow;

        // Step statuses do not include a distinct RevisionRequested value; the step
        // is closed as Rejected while the decision carries the RevisionRequested intent.
        active.Status = ApprovalStepStatuses.Rejected;
        active.CompletedAt = now;
        approval.Decisions.Add(BuildDecision(active.Id, ApprovalDecisions.RevisionRequested, comment, signatureMeaning, actor, now));
        approval.AuditEvents.Add(AuditFactory.Approval(ApprovalEventTypes.RevisionRequested, $"{active.StepName}: revizyon talep edildi.", actor, now));
        approval.TransitionTo(ApprovalStatuses.Rejected);
        approval.CompletedAt = now;
        approval.UpdatedAt = now;

        var change = await LoadChange(approval.RelatedObjectId);
        if (change is not null)
        {
            change.TransitionTo(ChangeStatuses.Draft);
            change.UpdatedAt = now;
            change.AuditEvents.Add(AuditFactory.Change(ChangeAuditEventTypes.ChangeRevisionRequested, $"Revizyon talep edildi ({approval.ApprovalNo}).", actor, now));
        }

        return new ApprovalActionResult(true, null);
    }

    /* ── Private helpers ─────────────────────────────────── */

    private sealed record StepGuard(ApprovalStep? Step, string? Error);

    /// <summary>
    /// Real role enforcement: the acting user must hold the active step's ApproverRole
    /// (and, for approve, the step-specific approve permission). If a specific approver
    /// is assigned, only that user may act. Authorization failures throw
    /// AuthForbiddenException (403); validation failures return an error (400).
    /// </summary>
    private StepGuard GuardActiveStep(ApprovalRequest approval, Guid actor, bool requireComment, string comment, bool requireApprovePermission)
    {
        if (approval.Status != ApprovalStatuses.InProgress)
            return new StepGuard(null, "Onay talebi işlemde (InProgress) değil.");

        var active = approval.Steps.FirstOrDefault(s => s.Status == ApprovalStepStatuses.Active);
        if (active is null)
            return new StepGuard(null, "İşlem yapılacak aktif adım bulunmuyor.");

        // Role enforcement: the user must hold the step's required role.
        if (!_currentUser.HasRole(active.ApproverRole))
            throw new AuthForbiddenException($"Bu adımı yalnızca '{active.ApproverRole}' rolündeki kullanıcı işleyebilir.");

        // Approve additionally requires the step-specific approve permission.
        if (requireApprovePermission)
        {
            var perm = ApprovePermissionFor(active.ApproverRole);
            if (perm is null || !_currentUser.HasPermission(perm))
                throw new AuthForbiddenException("Bu onay adımı için gerekli izne sahip değilsiniz.");
        }

        // If a specific approver is assigned, only that user may act.
        if (active.ApproverUserId.HasValue && active.ApproverUserId.Value != actor)
            throw new AuthForbiddenException("Bu adımı yalnızca atanmış onaycı işleyebilir.");

        if (requireComment && string.IsNullOrWhiteSpace(comment))
            return new StepGuard(null, "Bu işlem için yorum (comment) zorunludur.");

        return new StepGuard(active, null);
    }

    private Task<Guid?> ResolveApproverAsync(string role) =>
        _db.UserRoles.Where(ur => ur.Role!.Name == role)
            .Select(ur => (Guid?)ur.AppUserId)
            .FirstOrDefaultAsync();

    private Task<ChangeRequest?> LoadChange(Guid id) =>
        _db.ChangeRequests.Include(c => c.AuditEvents).FirstOrDefaultAsync(c => c.Id == id);

    private static ApprovalDecision BuildDecision(Guid stepId, string decision, string comment, string signatureMeaning, Guid userId, DateTime now) => new()
    {
        Id = Guid.NewGuid(), ApprovalStepId = stepId, Decision = decision,
        Comment = comment ?? string.Empty, SignatureMeaning = signatureMeaning ?? string.Empty,
        SignedByUserId = userId, SignedAt = now, CreatedAt = now
    };

    /// <summary>
    /// PART 3 — Change/Approval consistency. When a change is cancelled, every
    /// still-active approval (and its active/waiting steps) for that change is
    /// moved to Cancelled with an audit trail. Runs inside the caller's
    /// transaction (does NOT save) so the change cancel + approval cancel commit
    /// atomically.
    /// </summary>
    public async Task CancelForChangeAsync(Guid changeId, Guid actorUserId, DateTime now)
    {
        var activeApprovals = await _db.ApprovalRequests
            .Include(a => a.Steps)
            .Include(a => a.AuditEvents)
            .Where(a => a.RelatedObjectType == ApprovalRelatedObjectTypes.ChangeRequest
                        && a.RelatedObjectId == changeId
                        && a.Status == ApprovalStatuses.InProgress)
            .ToListAsync();

        foreach (var approval in activeApprovals)
        {
            foreach (var step in approval.Steps.Where(s =>
                         s.Status == ApprovalStepStatuses.Active || s.Status == ApprovalStepStatuses.Waiting))
            {
                step.Status = ApprovalStepStatuses.Cancelled;
                step.CompletedAt = now;
            }

            approval.TransitionTo(ApprovalStatuses.Cancelled);
            approval.CompletedAt = now;
            approval.UpdatedAt = now;
            approval.AuditEvents.Add(AuditFactory.Approval(ApprovalEventTypes.ApprovalCancelled,
                "İlişkili değişiklik iptal edildiği için onay talebi iptal edildi.", actorUserId, now));
        }
    }
}
