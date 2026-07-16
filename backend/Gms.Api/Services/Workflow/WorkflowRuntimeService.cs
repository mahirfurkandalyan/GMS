using System.Text.Json;
using Gms.Api.Common;
using Gms.Api.Data;
using Gms.Api.Domain;
using Gms.Api.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Services.Workflow;

/// <summary>
/// The workflow execution engine. It drives a <see cref="WorkflowInstance"/> through its
/// (immutable, published) version graph: automatic steps (Start/Condition/Notification/End) are
/// processed in a single bounded pass, while manual steps (ManualTask/Approval) pause the
/// instance until a human completes or rejects them. It also owns the Change integration
/// (Strategy A: the workflow is the single orchestrator — completion approves the change,
/// rejection sends it back). There is no dynamic code: routing goes through
/// <see cref="WorkflowConditionEvaluator"/> over allowlisted fields only.
/// </summary>
public sealed class WorkflowRuntimeService
{
    /// <summary>Hard upper bound on automatic steps processed in one pass (loop/runaway guard).</summary>
    private const int MaxAutomaticSteps = 50;

    private readonly GmsDbContext _db;
    private readonly SequentialNumberGenerator _numbers;
    private readonly NotificationService _notifications;
    private readonly ICurrentUser _currentUser;
    private readonly Integrations.IIntegrationEventPublisher _integrationEvents;

    public WorkflowRuntimeService(GmsDbContext db, SequentialNumberGenerator numbers,
        NotificationService notifications, ICurrentUser currentUser, Integrations.IIntegrationEventPublisher integrationEvents)
    {
        _db = db;
        _numbers = numbers;
        _notifications = notifications;
        _currentUser = currentUser;
        _integrationEvents = integrationEvents;
    }

    /* ── start (Change integration, Strategy A) ───────────── */

    /// <summary>
    /// Starts the governance workflow for a submitted change and moves the change to UnderReview.
    /// The change must be loaded with its Environment (for condition context). Adds to the tracked
    /// graph and auto-processes up to the first manual step; does NOT save — the caller (change
    /// submit) owns the transaction.
    /// </summary>
    public async Task<WorkflowInstance> StartForChangeAsync(ChangeRequest change, int readinessScore, Guid actor, CancellationToken ct = default)
    {
        var code = WorkflowCatalog.CodeForChangeClass(change.ChangeClass);
        var definition = await _db.WorkflowDefinitions
            .FirstOrDefaultAsync(d => d.Code == code && d.Status == WorkflowDefinitionStatuses.Active, ct)
            ?? throw new AuthValidationException($"'{code}' için aktif bir workflow tanımı bulunamadı.");
        if (definition.ActiveVersionId is not { } versionId)
            throw new AuthValidationException($"'{code}' workflow tanımının aktif bir sürümü yok.");

        var (steps, transitions) = await LoadGraphAsync(versionId, ct);

        var now = DateTime.UtcNow;
        var context = WorkflowConditionEvaluator.BuildChangeContext(change, readinessScore);
        var instanceNo = await _numbers.NextAsync($"WFI-{now.Year}-", _db.WorkflowInstances.Select(i => i.InstanceNo), ct);

        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(), InstanceNo = instanceNo,
            WorkflowDefinitionId = definition.Id, WorkflowVersionId = versionId,
            TriggerObjectType = WorkflowTriggers.ChangeRequestObject, TriggerObjectId = change.Id,
            TriggerObjectNumber = change.ChangeNo, Status = WorkflowInstanceStatuses.Created,
            RelatedProjectId = change.ProjectId, RelatedEnvironmentId = change.EnvironmentId,
            ContextJson = JsonSerializer.Serialize(context),
            StartedByUserId = actor, CreatedAt = now, StartedAt = now
        };
        instance.WorkflowDefinition = definition;
        instance.Events.Add(AuditFactory.Workflow(WorkflowEventTypes.WorkflowStarted,
            $"Workflow başlatıldı ({instanceNo}) — {definition.Name}.", actor, now));
        _db.WorkflowInstances.Add(instance);

        instance.TransitionTo(WorkflowInstanceStatuses.Running);

        // Change → UnderReview (the workflow now owns the change's review lifecycle).
        change.TransitionTo(ChangeStatuses.UnderReview);
        change.UpdatedAt = now;
        change.AuditEvents.Add(AuditFactory.Change(ChangeAuditEventTypes.ApprovalRequested,
            $"Onay akışı başlatıldı: {definition.Name} ({instanceNo}).", actor, now));

        var startKey = steps.First(s => s.StepType == WorkflowStepTypes.Start).StepKey;
        await DriveAsync(instance, definition, change, startKey, steps, transitions, context, actor, now, ct);

        return instance;
    }

    /* ── task actions ─────────────────────────────────────── */

    /// <summary>Completes the active manual/approval step (approve) and resumes the engine. Saves.</summary>
    public async Task<WorkflowInstance> CompleteTaskAsync(Guid instanceId, string? comment, CancellationToken ct = default)
    {
        var actor = _currentUser.RequireUserId();
        var instance = await LoadInstanceForActionAsync(instanceId, ct);
        var step = GuardActionableStep(instance, actor);

        var now = DateTime.UtcNow;
        step.Status = WorkflowStepStatuses.Completed;
        step.Result = step.StepType == WorkflowStepTypes.Approval ? WorkflowStepResults.Approved : WorkflowStepResults.Completed;
        step.ActionedByUserId = actor;
        step.Comment = comment;
        step.CompletedAt = now;
        instance.Events.Add(AuditFactory.Workflow(WorkflowEventTypes.StepCompleted,
            $"'{step.Name}' adımı tamamlandı.", actor, now, step.Id));

        instance.TransitionTo(WorkflowInstanceStatuses.Running);
        instance.CurrentStepInstanceId = null;

        var (steps, transitions) = await LoadGraphAsync(instance.WorkflowVersionId, ct);
        var context = DeserializeContext(instance.ContextJson);
        var change = await LoadChangeAsync(instance.TriggerObjectId, ct);

        var next = PickNext(step.StepKey, transitions, context);
        if (next is null)
        {
            await FailAsync(instance, change, $"'{step.StepKey}' adımından eşleşen geçiş bulunamadı.", actor, now);
        }
        else
        {
            await DriveAsync(instance, instance.WorkflowDefinition!, change, next, steps, transitions, context, actor, now, ct);
        }

        await _db.SaveChangesAsync(ct);
        return instance;
    }

    /// <summary>Rejects the active approval step; ends the instance and sends the change back. Saves.</summary>
    public async Task<WorkflowInstance> RejectTaskAsync(Guid instanceId, string? comment, CancellationToken ct = default)
    {
        var actor = _currentUser.RequireUserId();
        if (string.IsNullOrWhiteSpace(comment))
            throw new AuthValidationException("Reddetme işlemi için gerekçe (comment) zorunludur.");

        var instance = await LoadInstanceForActionAsync(instanceId, ct);
        var step = GuardActionableStep(instance, actor);
        if (step.StepType != WorkflowStepTypes.Approval)
            throw new AuthValidationException("Yalnızca onay (Approval) adımları reddedilebilir.");

        var now = DateTime.UtcNow;
        step.Status = WorkflowStepStatuses.Rejected;
        step.Result = WorkflowStepResults.Rejected;
        step.ActionedByUserId = actor;
        step.Comment = comment;
        step.CompletedAt = now;
        instance.Events.Add(AuditFactory.Workflow(WorkflowEventTypes.StepRejected,
            $"'{step.Name}' adımı reddedildi.", actor, now, step.Id));

        instance.CurrentStepInstanceId = null;
        instance.Outcome = $"'{step.Name}' adımında reddedildi.";
        instance.CompletedAt = now;
        instance.TransitionTo(WorkflowInstanceStatuses.Rejected);
        instance.Events.Add(AuditFactory.Workflow(WorkflowEventTypes.WorkflowFailed,
            "Workflow bir onay adımında reddedildi.", actor, now));

        var change = await LoadChangeAsync(instance.TriggerObjectId, ct);
        if (change is not null && change.Status == ChangeStatuses.UnderReview)
        {
            change.TransitionTo(ChangeStatuses.Submitted);
            change.UpdatedAt = now;
            change.AuditEvents.Add(AuditFactory.Change(ChangeAuditEventTypes.ChangeApprovalRejected,
                $"Onay akışı reddedildi ({instance.InstanceNo}).", actor, now));
            await _notifications.NotifyUserAsync(change.CreatedByUserId, NotificationTemplates.WorkflowRejected,
                NotificationSeverities.Error,
                new Dictionary<string, string> { ["ChangeNo"] = change.ChangeNo, ["StepName"] = step.Name }, actor, ct: ct);
        }

        await _db.SaveChangesAsync(ct);
        return instance;
    }

    /* ── lifecycle: cancel / pause / resume ───────────────── */

    /// <summary>Cancels a running/waiting instance and sends the change back to Submitted. Saves.</summary>
    public async Task<WorkflowInstance> CancelAsync(Guid instanceId, string? reason, string? rowVersion, CancellationToken ct = default)
    {
        var actor = _currentUser.RequireUserId();
        var instance = await LoadInstanceForActionAsync(instanceId, ct, requireWaiting: false);
        if (!string.IsNullOrWhiteSpace(rowVersion))
            _db.Entry(instance).Property(i => i.RowVersion).OriginalValue = Convert.FromBase64String(rowVersion);

        if (instance.Status is WorkflowInstanceStatuses.Completed or WorkflowInstanceStatuses.Rejected
            or WorkflowInstanceStatuses.Failed or WorkflowInstanceStatuses.Cancelled)
            throw new AuthValidationException("Sonlanmış bir workflow örneği iptal edilemez.");

        var now = DateTime.UtcNow;
        foreach (var s in instance.StepInstances.Where(s => s.Status == WorkflowStepStatuses.Active || s.Status == WorkflowStepStatuses.Waiting))
        {
            s.Status = WorkflowStepStatuses.Cancelled;
            s.CompletedAt = now;
        }
        instance.CurrentStepInstanceId = null;
        instance.Outcome = string.IsNullOrWhiteSpace(reason) ? "İptal edildi." : reason.Trim();
        instance.CompletedAt = now;
        instance.TransitionTo(WorkflowInstanceStatuses.Cancelled);
        instance.Events.Add(AuditFactory.Workflow(WorkflowEventTypes.WorkflowCancelled,
            $"Workflow iptal edildi. {instance.Outcome}", actor, now));

        var change = await LoadChangeAsync(instance.TriggerObjectId, ct);
        if (change is not null && change.Status == ChangeStatuses.UnderReview)
        {
            change.TransitionTo(ChangeStatuses.Submitted);
            change.UpdatedAt = now;
            change.AuditEvents.Add(AuditFactory.Change(ChangeAuditEventTypes.ChangeApprovalRejected,
                $"Onay akışı iptal edildi ({instance.InstanceNo}).", actor, now));
            await _notifications.NotifyUserAsync(change.CreatedByUserId, NotificationTemplates.WorkflowCancelled,
                NotificationSeverities.Warning,
                new Dictionary<string, string> { ["ChangeNo"] = change.ChangeNo, ["WorkflowName"] = instance.WorkflowDefinition?.Name ?? "" }, actor, ct: ct);
        }

        await _db.SaveChangesAsync(ct);
        return instance;
    }

    /// <summary>Pauses a Waiting instance (administrative hold). Saves.</summary>
    public async Task<WorkflowInstance> PauseAsync(Guid instanceId, CancellationToken ct = default)
    {
        var actor = _currentUser.RequireUserId();
        var instance = await LoadInstanceForActionAsync(instanceId, ct, requireWaiting: false);
        if (instance.Status != WorkflowInstanceStatuses.Waiting)
            throw new AuthValidationException("Yalnızca bekleyen (Waiting) workflow duraklatılabilir.");

        var now = DateTime.UtcNow;
        // Waiting → Running is a valid transition; we keep the active step but mark the instance
        // administratively paused via an audit event and hold it in Running (no auto-advance).
        instance.TransitionTo(WorkflowInstanceStatuses.Running);
        instance.Events.Add(AuditFactory.Workflow(WorkflowEventTypes.WorkflowPaused, "Workflow duraklatıldı.", actor, now));
        await _db.SaveChangesAsync(ct);
        return instance;
    }

    /// <summary>Resumes a paused instance back to Waiting on its active step. Saves.</summary>
    public async Task<WorkflowInstance> ResumeAsync(Guid instanceId, CancellationToken ct = default)
    {
        var actor = _currentUser.RequireUserId();
        var instance = await LoadInstanceForActionAsync(instanceId, ct, requireWaiting: false);
        if (instance.Status != WorkflowInstanceStatuses.Running)
            throw new AuthValidationException("Yalnızca duraklatılmış (Running) workflow sürdürülebilir.");

        var active = instance.StepInstances.FirstOrDefault(s => s.Status == WorkflowStepStatuses.Active);
        var now = DateTime.UtcNow;
        if (active is not null)
        {
            instance.CurrentStepInstanceId = active.Id;
            instance.TransitionTo(WorkflowInstanceStatuses.Waiting);
        }
        instance.Events.Add(AuditFactory.Workflow(WorkflowEventTypes.WorkflowResumed, "Workflow sürdürüldü.", actor, now));
        await _db.SaveChangesAsync(ct);
        return instance;
    }

    /// <summary>
    /// Cancels every still-running workflow instance for a change (used when the change itself is
    /// cancelled). Mutates the tracked graph only (does NOT save, does NOT touch change status) so
    /// it commits atomically inside the caller's change-cancel transaction.
    /// </summary>
    public async Task CancelForChangeAsync(Guid changeId, Guid actor, DateTime now, CancellationToken ct = default)
    {
        var instances = await _db.WorkflowInstances
            .Include(i => i.StepInstances)
            .Include(i => i.Events)
            .Where(i => i.TriggerObjectType == WorkflowTriggers.ChangeRequestObject && i.TriggerObjectId == changeId
                && (i.Status == WorkflowInstanceStatuses.Running || i.Status == WorkflowInstanceStatuses.Waiting
                    || i.Status == WorkflowInstanceStatuses.Created))
            .ToListAsync(ct);

        foreach (var instance in instances)
        {
            foreach (var s in instance.StepInstances.Where(s => s.Status == WorkflowStepStatuses.Active || s.Status == WorkflowStepStatuses.Waiting))
            {
                s.Status = WorkflowStepStatuses.Cancelled;
                s.CompletedAt = now;
            }
            instance.CurrentStepInstanceId = null;
            instance.Outcome = "İlişkili değişiklik iptal edildiği için iptal edildi.";
            instance.CompletedAt = now;
            instance.TransitionTo(WorkflowInstanceStatuses.Cancelled);
            instance.Events.Add(AuditFactory.Workflow(WorkflowEventTypes.WorkflowCancelled,
                "İlişkili değişiklik iptal edildiği için workflow iptal edildi.", actor, now));
        }
    }

    /* ── engine core ──────────────────────────────────────── */

    /// <summary>
    /// Bounded automatic pass starting at <paramref name="startKey"/>. Processes automatic steps in
    /// order and stops at the first manual step (pausing the instance) or at End (completing it).
    /// </summary>
    private async Task DriveAsync(WorkflowInstance instance, WorkflowDefinition definition, ChangeRequest? change,
        string startKey, IReadOnlyList<WorkflowStepDefinition> steps, IReadOnlyList<WorkflowTransitionDefinition> transitions,
        IReadOnlyDictionary<string, string> context, Guid actor, DateTime now, CancellationToken ct)
    {
        var byKey = steps.ToDictionary(s => s.StepKey, StringComparer.OrdinalIgnoreCase);
        var currentKey = startKey;
        var processed = 0;

        while (true)
        {
            if (++processed > MaxAutomaticSteps)
            {
                await FailAsync(instance, change, $"Otomatik adım sınırı ({MaxAutomaticSteps}) aşıldı.", actor, now);
                return;
            }

            if (!byKey.TryGetValue(currentKey, out var stepDef))
            {
                await FailAsync(instance, change, $"Adım tanımı bulunamadı: '{currentKey}'.", actor, now);
                return;
            }

            var si = NewStepInstance(instance, stepDef, now);
            instance.StepInstances.Add(si);

            // Manual step → activate and pause.
            if (WorkflowStepTypes.Manual.Contains(stepDef.StepType))
            {
                si.Status = WorkflowStepStatuses.Active;
                si.ActivatedAt = now;
                si.AssignedRole = stepDef.AssignedRole;
                si.AssignedUserId = stepDef.AssignedUserId ?? await ResolveRoleUserAsync(stepDef.AssignedRole, ct);
                if (stepDef.DueInHours is { } hrs) si.DueAt = now.AddHours(hrs);

                instance.CurrentStepInstanceId = si.Id;
                instance.TransitionTo(WorkflowInstanceStatuses.Waiting);
                instance.Events.Add(AuditFactory.Workflow(WorkflowEventTypes.StepActivated,
                    $"'{stepDef.Name}' adımı aktifleştirildi (atanan: {si.AssignedRole ?? "kullanıcı"}).", actor, now, si.Id));

                await NotifyAssigneeAsync(instance, definition, si, actor, ct);
                return; // pause
            }

            // Automatic step → complete immediately.
            si.Status = WorkflowStepStatuses.Completed;
            si.Result = WorkflowStepResults.Auto;
            si.ActivatedAt = now;
            si.CompletedAt = now;

            if (stepDef.StepType == WorkflowStepTypes.End)
            {
                instance.Events.Add(AuditFactory.Workflow(WorkflowEventTypes.StepCompleted, $"'{stepDef.Name}' (End).", actor, now, si.Id));
                await CompleteInstanceAsync(instance, definition, change, actor, now, ct);
                return;
            }

            if (stepDef.StepType == WorkflowStepTypes.Notification)
            {
                instance.Events.Add(AuditFactory.Workflow(WorkflowEventTypes.NotificationTriggered,
                    $"Bildirim adımı '{stepDef.Name}' tetiklendi.", actor, now, si.Id));
                await FireNotificationStepAsync(instance, definition, change, stepDef, actor, ct);
            }
            else if (stepDef.StepType == WorkflowStepTypes.Condition)
            {
                instance.Events.Add(AuditFactory.Workflow(WorkflowEventTypes.ConditionEvaluated,
                    $"Koşul adımı '{stepDef.Name}' değerlendirildi.", actor, now, si.Id));
            }
            else // Start
            {
                instance.Events.Add(AuditFactory.Workflow(WorkflowEventTypes.StepCompleted, $"'{stepDef.Name}' (Start).", actor, now, si.Id));
            }

            var next = PickNext(stepDef.StepKey, transitions, context);
            if (next is null)
            {
                await FailAsync(instance, change, $"'{stepDef.StepKey}' adımından eşleşen geçiş bulunamadı.", actor, now);
                return;
            }
            currentKey = next;
        }
    }

    /// <summary>First outgoing transition (ascending priority) whose condition matches.</summary>
    private static string? PickNext(string fromStepKey, IReadOnlyList<WorkflowTransitionDefinition> transitions,
        IReadOnlyDictionary<string, string> context)
    {
        var outgoing = transitions
            .Where(t => string.Equals(t.FromStepKey, fromStepKey, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Priority);
        foreach (var t in outgoing)
            if (WorkflowConditionEvaluator.Evaluate(context, t))
                return t.ToStepKey;
        return null;
    }

    private async Task CompleteInstanceAsync(WorkflowInstance instance, WorkflowDefinition definition,
        ChangeRequest? change, Guid actor, DateTime now, CancellationToken ct)
    {
        instance.CurrentStepInstanceId = null;
        instance.Outcome = "Tamamlandı (onaylandı).";
        instance.CompletedAt = now;
        instance.TransitionTo(WorkflowInstanceStatuses.Completed);
        instance.Events.Add(AuditFactory.Workflow(WorkflowEventTypes.WorkflowCompleted,
            "Workflow tüm adımları tamamlandı.", actor, now));

        if (change is not null && change.Status == ChangeStatuses.UnderReview)
        {
            change.TransitionTo(ChangeStatuses.Approved);
            change.UpdatedAt = now;
            change.AuditEvents.Add(AuditFactory.Change(ChangeAuditEventTypes.ChangeApproved,
                $"Değişiklik onay akışıyla onaylandı ({instance.InstanceNo}).", actor, now));
            await _notifications.NotifyUserAsync(change.CreatedByUserId, NotificationTemplates.WorkflowCompleted,
                NotificationSeverities.Success,
                new Dictionary<string, string> { ["ChangeNo"] = change.ChangeNo, ["WorkflowName"] = definition.Name }, actor, ct: ct);
        }

        // Integration Hub seam: enqueue outgoing deliveries for the WorkflowCompleted event. This
        // only adds Pending IntegrationExecution rows to the tracked graph (no HTTP, no save) so
        // deliveries commit atomically with this transaction; the dispatcher performs them later.
        await _integrationEvents.PublishAsync(IntegrationSubscriptionEvents.WorkflowCompleted,
            "WorkflowInstance", instance.Id,
            new { instance.InstanceNo, WorkflowName = definition.Name, ChangeNo = change?.ChangeNo, ObjectId = instance.Id },
            actor, ct);
    }

    private async Task FailAsync(WorkflowInstance instance, ChangeRequest? change, string reason, Guid actor, DateTime now)
    {
        instance.CurrentStepInstanceId = null;
        instance.Outcome = reason;
        instance.CompletedAt = now;
        instance.TransitionTo(WorkflowInstanceStatuses.Failed);
        instance.Events.Add(AuditFactory.Workflow(WorkflowEventTypes.WorkflowFailed, reason, actor, now));
        await Task.CompletedTask;
    }

    private async Task FireNotificationStepAsync(WorkflowInstance instance, WorkflowDefinition definition,
        ChangeRequest? change, WorkflowStepDefinition stepDef, Guid actor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(stepDef.NotificationTemplateCode)) return;
        var data = new Dictionary<string, string>
        {
            ["ChangeNo"] = instance.TriggerObjectNumber ?? string.Empty,
            ["WorkflowName"] = definition.Name,
            ["StepName"] = stepDef.Name
        };
        if (!string.IsNullOrWhiteSpace(stepDef.NotificationRole))
            await _notifications.NotifyRoleAsync(stepDef.NotificationRole, stepDef.NotificationTemplateCode,
                NotificationSeverities.Information, data, actor, ct);
        else if (change is not null)
            await _notifications.NotifyUserAsync(change.CreatedByUserId, stepDef.NotificationTemplateCode,
                NotificationSeverities.Information, data, actor, ct: ct);
    }

    private async Task NotifyAssigneeAsync(WorkflowInstance instance, WorkflowDefinition definition,
        WorkflowStepInstance step, Guid actor, CancellationToken ct)
    {
        var data = new Dictionary<string, string>
        {
            ["StepName"] = step.Name,
            ["ChangeNo"] = instance.TriggerObjectNumber ?? string.Empty,
            ["WorkflowName"] = definition.Name,
            ["DueAt"] = step.DueAt?.ToString("dd.MM.yyyy HH:mm") ?? "-"
        };
        if (step.AssignedUserId is { } uid)
            await _notifications.NotifyUserAsync(uid, NotificationTemplates.WorkflowTaskAssigned,
                NotificationSeverities.Warning, data, actor, ct: ct);
        else if (!string.IsNullOrWhiteSpace(step.AssignedRole))
            await _notifications.NotifyRoleAsync(step.AssignedRole, NotificationTemplates.WorkflowTaskAssigned,
                NotificationSeverities.Warning, data, actor, ct);
    }

    /* ── helpers ──────────────────────────────────────────── */

    private static WorkflowStepInstance NewStepInstance(WorkflowInstance instance, WorkflowStepDefinition def, DateTime now) => new()
    {
        Id = Guid.NewGuid(), WorkflowInstanceId = instance.Id, StepDefinitionId = def.Id,
        StepKey = def.StepKey, Name = def.Name, StepType = def.StepType, StepOrder = def.StepOrder,
        Status = WorkflowStepStatuses.Waiting, AssignedRole = def.AssignedRole, AssignedUserId = def.AssignedUserId,
        CreatedAt = now
    };

    /// <summary>Guards a task action: instance Waiting, an Active manual step, and actor is authorised.</summary>
    private WorkflowStepInstance GuardActionableStep(WorkflowInstance instance, Guid actor)
    {
        if (instance.Status != WorkflowInstanceStatuses.Waiting)
            throw new AuthValidationException("Workflow bir görevi bekleyen (Waiting) durumda değil.");

        var step = instance.StepInstances.FirstOrDefault(s => s.Status == WorkflowStepStatuses.Active)
            ?? throw new AuthValidationException("İşlem yapılacak aktif adım bulunmuyor.");

        // Admin override bypasses assignment; otherwise the actor must match the assignment.
        if (_currentUser.HasPermission(Permissions.WorkflowAdminOverride))
            return step;

        if (step.AssignedUserId is { } uid && uid != actor)
            throw new AuthForbiddenException("Bu görev başka bir kullanıcıya atanmış.");
        if (step.AssignedUserId is null && !string.IsNullOrWhiteSpace(step.AssignedRole) && !_currentUser.HasRole(step.AssignedRole))
            throw new AuthForbiddenException($"Bu görevi yalnızca '{step.AssignedRole}' rolündeki kullanıcı işleyebilir.");

        return step;
    }

    private async Task<WorkflowInstance> LoadInstanceForActionAsync(Guid instanceId, CancellationToken ct, bool requireWaiting = true)
    {
        var instance = await _db.WorkflowInstances
            .Include(i => i.WorkflowDefinition)
            .Include(i => i.StepInstances)
            .Include(i => i.Events)
            .FirstOrDefaultAsync(i => i.Id == instanceId, ct)
            ?? throw new KeyNotFoundException("Workflow örneği bulunamadı.");
        if (requireWaiting && instance.Status != WorkflowInstanceStatuses.Waiting)
            throw new AuthValidationException("Workflow bir görevi bekleyen (Waiting) durumda değil.");
        return instance;
    }

    private Task<ChangeRequest?> LoadChangeAsync(Guid changeId, CancellationToken ct) =>
        _db.ChangeRequests.Include(c => c.AuditEvents).FirstOrDefaultAsync(c => c.Id == changeId, ct);

    private async Task<(IReadOnlyList<WorkflowStepDefinition> Steps, IReadOnlyList<WorkflowTransitionDefinition> Transitions)>
        LoadGraphAsync(Guid versionId, CancellationToken ct)
    {
        var steps = await _db.WorkflowStepDefinitions.AsNoTracking()
            .Where(s => s.WorkflowVersionId == versionId).OrderBy(s => s.StepOrder).ToListAsync(ct);
        var transitions = await _db.WorkflowTransitionDefinitions.AsNoTracking()
            .Where(t => t.WorkflowVersionId == versionId).OrderBy(t => t.Priority).ToListAsync(ct);
        return (steps, transitions);
    }

    private Task<Guid?> ResolveRoleUserAsync(string? role, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(role)) return Task.FromResult<Guid?>(null);
        return _db.UserRoles.Where(ur => ur.Role!.Name == role && ur.AppUser!.IsActive)
            .Select(ur => (Guid?)ur.AppUserId).FirstOrDefaultAsync(ct);
    }

    private static IReadOnlyDictionary<string, string> DeserializeContext(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        return new Dictionary<string, string>(parsed ?? new(), StringComparer.OrdinalIgnoreCase);
    }
}
