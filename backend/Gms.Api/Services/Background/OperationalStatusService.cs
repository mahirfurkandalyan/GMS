using Gms.Api.Common;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Services.Background;

/// <summary>
/// Read-only operational status: backlog counts and worker heartbeats. Used by the operations
/// endpoint and the readiness health check. Pure counting queries (AsNoTracking); no secrets.
/// </summary>
public interface IOperationalStatusService
{
    Task<OperationalStatusDto> GetAsync(CancellationToken ct = default);
    Task<int> IntegrationBacklogAsync(CancellationToken ct = default);
    Task<int> NotificationBacklogAsync(CancellationToken ct = default);
}

public sealed class OperationalStatusService : IOperationalStatusService
{
    private readonly GmsDbContext _db;
    public OperationalStatusService(GmsDbContext db) => _db = db;

    public async Task<OperationalStatusDto> GetAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var dto = new OperationalStatusDto
        {
            PendingIntegrationExecutions = await _db.IntegrationExecutions.AsNoTracking()
                .CountAsync(x => x.Status == IntegrationExecutionStatuses.Pending, ct),
            RetryScheduledIntegrationExecutions = await _db.IntegrationExecutions.AsNoTracking()
                .CountAsync(x => x.Status == IntegrationExecutionStatuses.Failed, ct),
            DeadLetterIntegrationExecutions = await _db.IntegrationExecutions.AsNoTracking()
                .CountAsync(x => x.Status == IntegrationExecutionStatuses.DeadLetter, ct),
            PendingNotificationDeliveries = await _db.NotificationDeliveries.AsNoTracking()
                .CountAsync(d => d.Channel == NotificationChannels.Email && d.Status == NotificationDeliveryStatuses.Pending, ct),
            FailedNotificationDeliveries = await _db.NotificationDeliveries.AsNoTracking()
                .CountAsync(d => d.Channel == NotificationChannels.Email
                    && (d.Status == NotificationDeliveryStatuses.Failed || d.Status == NotificationDeliveryStatuses.DeadLetter), ct),
            OverdueWorkflowTasks = await _db.WorkflowStepInstances.AsNoTracking()
                .CountAsync(s => s.Status == WorkflowStepStatuses.Active && s.DueAt != null && s.DueAt < now, ct),
            GeneratedAt = now
        };

        dto.Workers = await _db.WorkerHeartbeats.AsNoTracking()
            .OrderBy(h => h.WorkerName)
            .Select(h => new WorkerHeartbeatDto
            {
                WorkerName = h.WorkerName, InstanceId = h.InstanceId,
                LastStartedAt = h.LastStartedAt, LastSucceededAt = h.LastSucceededAt,
                LastFailedAt = h.LastFailedAt, LastError = h.LastError, UpdatedAt = h.UpdatedAt
            }).ToListAsync(ct);

        return dto;
    }

    public Task<int> IntegrationBacklogAsync(CancellationToken ct = default) =>
        _db.IntegrationExecutions.AsNoTracking().CountAsync(x => x.Status == IntegrationExecutionStatuses.Pending, ct);

    public Task<int> NotificationBacklogAsync(CancellationToken ct = default) =>
        _db.NotificationDeliveries.AsNoTracking()
            .CountAsync(d => d.Channel == NotificationChannels.Email && d.Status == NotificationDeliveryStatuses.Pending, ct);
}
