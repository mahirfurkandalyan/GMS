using System.Diagnostics;
using System.Text.Json;
using Gms.Api.Common;
using Gms.Api.Common.Observability;
using Gms.Api.Contracts;
using Gms.Api.Data;
using Gms.Api.Domain;
using Gms.Api.Services.Integrations.Providers;
using Gms.Api.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Services.Integrations;

/// <summary>
/// Processes the integration outbox: fetches dispatchable executions (Pending/Failed), runs the
/// provider once per pass, records an attempt, and drives the execution to Succeeded / Failed
/// (retryable) / DeadLetter per the retry policy. Notifies admins on dead-letter. This service
/// self-saves. In tests, dispatch is Admin-triggered (no uncontrolled polling).
/// </summary>
public interface IIntegrationDispatcher
{
    Task<DispatchResultDto> DispatchPendingAsync(int max, Guid? actorUserId, CancellationToken ct = default);
    Task DispatchOneAsync(Guid executionId, Guid? actorUserId, CancellationToken ct = default);
    /// <summary>Atomically leases a bounded batch of dispatchable executions for one worker instance.</summary>
    Task<IReadOnlyList<Guid>> ClaimDispatchableAsync(string owner, TimeSpan lease, int batch, CancellationToken ct = default);
}

public sealed class IntegrationDispatcher : IIntegrationDispatcher
{
    private readonly GmsDbContext _db;
    private readonly IIntegrationProviderResolver _resolver;
    private readonly ISecretProtector _secrets;
    private readonly NotificationService _notifications;
    private readonly IIntegrationDelayStrategy _delay;

    public IntegrationDispatcher(GmsDbContext db, IIntegrationProviderResolver resolver, ISecretProtector secrets,
        NotificationService notifications, IIntegrationDelayStrategy delay)
    {
        _db = db;
        _resolver = resolver;
        _secrets = secrets;
        _notifications = notifications;
        _delay = delay;
    }

    public async Task<DispatchResultDto> DispatchPendingAsync(int max, Guid? actorUserId, CancellationToken ct = default)
    {
        var limit = Math.Clamp(max, 1, 200);
        var now = DateTime.UtcNow;
        var ids = await _db.IntegrationExecutions.AsNoTracking()
            .Where(x => x.Direction == IntegrationDirections.Outgoing
                && (x.Status == IntegrationExecutionStatuses.Pending
                    || (x.Status == IntegrationExecutionStatuses.Failed
                        && (x.NextAttemptAt == null || x.NextAttemptAt <= now))))
            .OrderBy(x => x.CreatedAt).Take(limit)
            .Select(x => x.Id).ToListAsync(ct);

        var result = new DispatchResultDto();
        foreach (var id in ids)
        {
            var outcome = await ProcessAsync(id, actorUserId, ct);
            result.Processed++;
            switch (outcome)
            {
                case IntegrationExecutionStatuses.Succeeded: result.Succeeded++; break;
                case IntegrationExecutionStatuses.DeadLetter: result.DeadLettered++; break;
                case IntegrationExecutionStatuses.Failed: result.Failed++; break;
            }
        }
        return result;
    }

    /// <summary>
    /// Atomically claims a bounded batch of dispatchable executions for one worker instance by
    /// setting a lease (LockedUntil/LockedBy) with RowVersion concurrency. Rows already leased
    /// (LockedUntil in the future) or not yet due (NextAttemptAt in the future) are skipped. Returns
    /// the claimed ids. Two worker instances cannot claim the same row (optimistic-concurrency race
    /// resolves to exactly one winner).
    /// </summary>
    public async Task<IReadOnlyList<Guid>> ClaimDispatchableAsync(string owner, TimeSpan lease, int batch, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var candidates = await _db.IntegrationExecutions
            .Where(x => x.Direction == IntegrationDirections.Outgoing
                && (x.Status == IntegrationExecutionStatuses.Pending
                    || (x.Status == IntegrationExecutionStatuses.Failed
                        && (x.NextAttemptAt == null || x.NextAttemptAt <= now)))
                && (x.LockedUntil == null || x.LockedUntil < now))
            .OrderBy(x => x.CreatedAt).Take(batch)
            .ToListAsync(ct);

        var claimed = new List<Guid>();
        foreach (var exec in candidates)
        {
            exec.LockedUntil = now.Add(lease);
            exec.LockedBy = owner;
            try
            {
                await _db.SaveChangesAsync(ct);
                claimed.Add(exec.Id);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another worker claimed this row first — drop it and reset tracking state.
                _db.Entry(exec).State = EntityState.Detached;
            }
        }
        return claimed;
    }

    public async Task DispatchOneAsync(Guid executionId, Guid? actorUserId, CancellationToken ct = default)
        => await ProcessAsync(executionId, actorUserId, ct);

    /// <summary>Runs one execution attempt end-to-end and persists the outcome. Returns the resulting status.</summary>
    private async Task<string> ProcessAsync(Guid executionId, Guid? actorUserId, CancellationToken ct)
    {
        var exec = await _db.IntegrationExecutions
            .Include(x => x.IntegrationDefinition).ThenInclude(d => d!.Credentials)
            .Include(x => x.Attempts)
            .FirstOrDefaultAsync(x => x.Id == executionId, ct);
        if (exec is null) return "NotFound";
        if (IntegrationExecutionStatuses.Terminal.Contains(exec.Status)) return exec.Status;

        var def = exec.IntegrationDefinition!;
        var now = DateTime.UtcNow;
        var attemptNo = exec.Attempts.Count + 1;

        exec.TransitionTo(IntegrationExecutionStatuses.Running);
        exec.StartedAt ??= now;

        // Decrypt credentials only here (inside execution); never logged.
        var creds = DecryptCredentials(def);
        var provider = _resolver.Resolve(def.Provider);
        var endpoint = await _db.IntegrationEndpoints.AsNoTracking()
            .Where(e => e.IntegrationDefinitionId == def.Id && e.IsActive && e.Direction == IntegrationDirections.Outgoing)
            .OrderBy(e => e.CreatedAt).FirstOrDefaultAsync(ct);

        var payload = ParsePayload(exec.RequestSummary);
        var sw = Stopwatch.StartNew();
        ProviderExecuteResult res;
        try
        {
            res = await provider.ExecuteAsync(new ProviderExecuteRequest
            {
                Definition = def, Endpoint = endpoint, Operation = exec.Operation,
                CorrelationId = exec.CorrelationId, DecryptedCredentials = creds, Payload = payload
            }, ct);
        }
        catch (Exception ex)
        {
            res = ProviderExecuteResult.Fail(null, "PROVIDER_ERROR", ex.Message, transient: true);
        }
        sw.Stop();

        exec.Attempts.Add(new IntegrationExecutionAttempt
        {
            Id = Guid.NewGuid(), IntegrationExecutionId = exec.Id, AttemptNo = attemptNo,
            StartedAt = now, CompletedAt = DateTime.UtcNow,
            Status = res.Success ? "Succeeded" : "Failed", HttpStatusCode = res.HttpStatusCode,
            ErrorMessage = Trim(res.ErrorMessage, 1000), DurationMilliseconds = (int)sw.ElapsedMilliseconds, CreatedAt = now
        });

        exec.HttpStatusCode = res.HttpStatusCode;
        exec.ResponseSummary = res.ResponseSummary is null ? exec.ResponseSummary : IntegrationHttpClient.Sanitize(res.ResponseSummary);

        GmsTelemetry.IntegrationExecutions.Add(1, GmsTelemetry.Provider(def.Provider), GmsTelemetry.Result(res.Success ? "success" : "failure"));

        if (res.Success)
        {
            exec.TransitionTo(IntegrationExecutionStatuses.Succeeded);
            exec.CompletedAt = DateTime.UtcNow;
            exec.ErrorCode = null; exec.ErrorMessage = null;
            ClearLease(exec);
            exec.Events.Add(AuditFactory.Integration(IntegrationEventTypes.OutgoingDeliverySucceeded,
                $"Giden teslimat başarılı (deneme {attemptNo}).", def.Id, actorUserId, DateTime.UtcNow, exec.Id));
            await _db.SaveChangesAsync(ct);
            return IntegrationExecutionStatuses.Succeeded;
        }

        // Failure path.
        GmsTelemetry.IntegrationFailures.Add(1, GmsTelemetry.Provider(def.Provider));
        exec.RetryCount = attemptNo;
        exec.ErrorCode = res.ErrorCode;
        exec.ErrorMessage = Trim(res.ErrorMessage, 1000);
        exec.TransitionTo(IntegrationExecutionStatuses.Failed);
        exec.Events.Add(AuditFactory.Integration(IntegrationEventTypes.OutgoingDeliveryFailed,
            $"Giden teslimat başarısız (deneme {attemptNo}): {exec.ErrorCode}.", def.Id, actorUserId, DateTime.UtcNow, exec.Id));

        var reachedLimit = attemptNo >= IntegrationRetry.MaxAttempts;
        if (!res.IsTransient || reachedLimit)
        {
            exec.TransitionTo(IntegrationExecutionStatuses.DeadLetter);
            exec.CompletedAt = DateTime.UtcNow;
            exec.NextAttemptAt = null;
            ClearLease(exec);
            GmsTelemetry.IntegrationDeadLetters.Add(1, GmsTelemetry.Provider(def.Provider));
            var reason = res.IsTransient ? "azami deneme aşıldı" : "kalıcı hata";
            exec.Events.Add(AuditFactory.Integration(IntegrationEventTypes.DeadLettered,
                $"Yürütme ölü mektup kutusuna alındı ({reason}).", def.Id, actorUserId, DateTime.UtcNow, exec.Id));
            await NotifyAdminsAsync(NotificationTemplates.IntegrationDeadLettered, def, exec, ct);
            await _db.SaveChangesAsync(ct);
            return IntegrationExecutionStatuses.DeadLetter;
        }

        // Transient, under the limit → keep Failed (retryable) and schedule the next attempt (backoff).
        var delay = _delay.NextDelay(attemptNo);
        exec.NextAttemptAt = DateTime.UtcNow.Add(delay);
        ClearLease(exec); // release the lease so it can be reclaimed once NextAttemptAt is due
        exec.Events.Add(AuditFactory.Integration(IntegrationEventTypes.RetryScheduled,
            $"Yeniden deneme planlandı (~{(int)delay.TotalSeconds} sn sonra).", def.Id, actorUserId, DateTime.UtcNow, exec.Id));
        await _db.SaveChangesAsync(ct);
        return IntegrationExecutionStatuses.Failed;
    }

    private static void ClearLease(IntegrationExecution exec)
    {
        exec.LockedUntil = null;
        exec.LockedBy = null;
    }

    private Dictionary<string, string> DecryptCredentials(IntegrationDefinition def)
    {
        var map = new Dictionary<string, string>();
        foreach (var c in def.Credentials)
        {
            try { map[c.KeyName] = _secrets.Unprotect(c.EncryptedValue); }
            catch { /* undecryptable key (e.g. key-ring reset) → skip; provider will fail auth */ }
        }
        return map;
    }

    private static object? ParsePayload(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonDocument.Parse(json).RootElement.Clone(); }
        catch { return null; }
    }

    private async Task NotifyAdminsAsync(string template, IntegrationDefinition def, IntegrationExecution exec, CancellationToken ct)
    {
        await _notifications.NotifyRoleAsync(SystemRoles.Admin, template, NotificationSeverities.Error,
            new Dictionary<string, string>
            {
                ["IntegrationNo"] = def.IntegrationNo, ["IntegrationName"] = def.Name,
                ["ExecutionNo"] = exec.ExecutionNo, ["Error"] = exec.ErrorMessage ?? exec.ErrorCode ?? "bilinmiyor"
            }, null, ct);
    }

    private static string? Trim(string? v, int max) => v is null ? null : (v.Length <= max ? v : v[..max]);
}
