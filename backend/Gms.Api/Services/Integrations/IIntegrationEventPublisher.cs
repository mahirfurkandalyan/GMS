using System.Text.Json;
using Gms.Api.Common;
using Gms.Api.Data;
using Gms.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Services.Integrations;

/// <summary>
/// The central seam through which GMS domains hand meaningful events to the Integration Hub. It
/// creates <c>Pending</c> <see cref="IntegrationExecution"/> records for each active outgoing
/// subscription — WITHOUT saving — so they commit inside the caller's business transaction and the
/// external HTTP side effect never happens before the database commit (no ghost deliveries on
/// rollback). Actual delivery is performed later by <see cref="IIntegrationDispatcher"/>.
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>Enqueues outgoing deliveries for an event. Adds to the tracked graph; does NOT save. Returns the count created.</summary>
    Task<int> PublishAsync(string eventType, string? objectType, Guid? objectId, object? payload, Guid? actorUserId, CancellationToken ct = default);
}

public sealed class IntegrationEventPublisher : IIntegrationEventPublisher
{
    private const int MaxPayloadChars = 1800;
    private readonly GmsDbContext _db;
    public IntegrationEventPublisher(GmsDbContext db) => _db = db;

    public async Task<int> PublishAsync(string eventType, string? objectType, Guid? objectId, object? payload, Guid? actorUserId, CancellationToken ct = default)
    {
        if (!IntegrationSubscriptionEvents.All.Contains(eventType)) return 0;

        // Active outgoing subscriptions for this event on Active integrations.
        var targets = await _db.IntegrationSubscriptions.AsNoTracking()
            .Where(s => s.IsActive && s.EventType == eventType
                && s.IntegrationDefinition!.Status == IntegrationStatuses.Active)
            .Select(s => new { s.IntegrationDefinitionId, s.TargetEndpointId })
            .ToListAsync(ct);
        if (targets.Count == 0) return 0;

        var now = DateTime.UtcNow;
        var json = Truncate(payload is null ? "{}" : JsonSerializer.Serialize(payload), MaxPayloadChars);
        var count = 0;

        foreach (var t in targets)
        {
            // Only enqueue when the target endpoint exists, is Outgoing and active.
            var endpointOk = await _db.IntegrationEndpoints.AsNoTracking()
                .AnyAsync(e => e.Id == t.TargetEndpointId && e.IsActive && e.Direction == IntegrationDirections.Outgoing, ct);
            if (!endpointOk) continue;

            var execNo = await NextExecutionNoAsync(now.Year, ct);
            _db.IntegrationExecutions.Add(new IntegrationExecution
            {
                Id = Guid.NewGuid(),
                ExecutionNo = execNo,
                IntegrationDefinitionId = t.IntegrationDefinitionId,
                Direction = IntegrationDirections.Outgoing,
                Operation = eventType,
                ObjectType = objectType,
                ObjectId = objectId,
                CorrelationId = Guid.NewGuid().ToString("N"),
                Status = IntegrationExecutionStatuses.Pending,
                RequestSummary = json, // non-secret, length-limited outbound payload (delivered at dispatch)
                CreatedByUserId = actorUserId,
                CreatedAt = now
            });
            count++;
        }
        return count;
    }

    /// <summary>Batch-safe INX number allocation (considers saved + pending Added rows).</summary>
    private async Task<string> NextExecutionNoAsync(int year, CancellationToken ct)
    {
        var prefix = $"INX-{year}-";
        var maxSaved = await _db.IntegrationExecutions.Where(x => x.ExecutionNo.StartsWith(prefix))
            .Select(x => x.ExecutionNo).OrderByDescending(x => x).FirstOrDefaultAsync(ct);
        var maxPending = _db.ChangeTracker.Entries<IntegrationExecution>()
            .Where(e => e.State == EntityState.Added && e.Entity.ExecutionNo.StartsWith(prefix))
            .Select(e => e.Entity.ExecutionNo).OrderByDescending(x => x).FirstOrDefault();

        var max = new[] { maxSaved, maxPending }.Where(x => !string.IsNullOrEmpty(x)).DefaultIfEmpty(string.Empty).Max();
        var next = 1;
        if (!string.IsNullOrEmpty(max) && int.TryParse(max![prefix.Length..], out var parsed)) next = parsed + 1;
        return $"{prefix}{next:000000}";
    }

    private static string Truncate(string v, int max) => v.Length <= max ? v : v[..max];
}
