using Gms.Api.Common;
using Gms.Api.Common.Observability;
using Gms.Api.Data;
using Gms.Api.Domain;
using Gms.Api.Services;
using Gms.Api.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gms.Api.Services.Background;

/// <summary>
/// Periodically detects workflow tasks that are due soon or overdue (via
/// <see cref="WorkflowStepInstance.DueAt"/>) and generates reminders through the Notification
/// Engine. Reminders are de-duplicated per step via DueSoonNotifiedAt/OverdueNotifiedAt plus a
/// configurable cooldown, so they never repeat endlessly. Tasks are NEVER auto-completed,
/// auto-rejected or auto-escalated — this is reminder-only (escalation foundation for later).
/// </summary>
public sealed class WorkflowSlaWorker : BackgroundWorkerBase
{
    private readonly WorkflowSlaOptions _opts;

    public WorkflowSlaWorker(IServiceScopeFactory scopeFactory, ILogger<WorkflowSlaWorker> logger,
        IOptions<BackgroundProcessingOptions> options) : base(scopeFactory, logger, options)
        => _opts = options.Value.WorkflowSla;

    public override string WorkerName => "WorkflowSla";
    protected override bool WorkerEnabled => _opts.Enabled;
    protected override TimeSpan PollInterval => _opts.PollInterval;

    protected override async Task<int> RunCycleAsync(IServiceScope scope, CancellationToken ct)
    {
        using var activity = GmsTelemetry.ActivitySource.StartActivity("workflow.sla.cycle");
        var db = scope.ServiceProvider.GetRequiredService<GmsDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<NotificationService>();

        var now = DateTime.UtcNow;
        var dueSoonCutoff = now.AddHours(_opts.DueSoonHours);
        var cooldownCutoff = now.AddHours(-_opts.ReminderCooldownHours);

        // Keep the overdue gauge fresh (bounded count query).
        var overdueTotal = await db.WorkflowStepInstances.AsNoTracking()
            .CountAsync(s => s.Status == WorkflowStepStatuses.Active && s.DueAt != null && s.DueAt < now, ct);
        GmsTelemetry.SetOverdueTasks(overdueTotal);

        var steps = await db.WorkflowStepInstances
            .Include(s => s.WorkflowInstance).ThenInclude(i => i!.WorkflowDefinition)
            .Where(s => s.Status == WorkflowStepStatuses.Active && s.DueAt != null
                && s.WorkflowInstance!.Status == WorkflowInstanceStatuses.Waiting
                && ((s.DueAt < now && (s.OverdueNotifiedAt == null || s.OverdueNotifiedAt < cooldownCutoff))
                    || (s.DueAt >= now && s.DueAt <= dueSoonCutoff && (s.DueSoonNotifiedAt == null || s.DueSoonNotifiedAt < cooldownCutoff))))
            .OrderBy(s => s.DueAt).Take(_opts.Batch)
            .ToListAsync(ct);

        var processed = 0;
        foreach (var step in steps)
        {
            if (ct.IsCancellationRequested) break;
            var overdue = step.DueAt < now;
            var template = overdue ? NotificationTemplates.WorkflowTaskOverdue : NotificationTemplates.WorkflowTaskDueSoon;
            var data = new Dictionary<string, string>
            {
                ["StepName"] = step.Name,
                ["ChangeNo"] = step.WorkflowInstance?.TriggerObjectNumber ?? string.Empty,
                ["WorkflowName"] = step.WorkflowInstance?.WorkflowDefinition?.Name ?? string.Empty,
                ["DueAt"] = step.DueAt?.ToString("dd.MM.yyyy HH:mm") ?? "-"
            };

            if (step.AssignedUserId is { } uid)
                await notifications.NotifyUserAsync(uid, template, NotificationSeverities.Warning, data, null, ct: ct);
            else if (!string.IsNullOrWhiteSpace(step.AssignedRole))
                await notifications.NotifyRoleAsync(step.AssignedRole, template, NotificationSeverities.Warning, data, null, ct);

            if (overdue) step.OverdueNotifiedAt = now; else step.DueSoonNotifiedAt = now;

            // Audit the reminder generation (system actor).
            db.WorkflowEvents.Add(new WorkflowEvent
            {
                Id = Guid.NewGuid(), WorkflowInstanceId = step.WorkflowInstanceId, WorkflowStepInstanceId = step.Id,
                ActorUserId = Guid.Empty, EventType = WorkflowEventTypes.SlaReminderSent,
                Description = overdue ? $"Gecikme hatırlatması: '{step.Name}'." : $"Süre yaklaşıyor hatırlatması: '{step.Name}'.",
                CreatedAt = now
            });
            processed++;
        }

        if (processed > 0) await db.SaveChangesAsync(ct);
        activity?.SetTag("gms.reminders", processed);
        return processed;
    }
}
