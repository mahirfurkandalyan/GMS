using System.Diagnostics;
using Gms.Api.Common.Observability;
using Gms.Api.Data;
using Gms.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gms.Api.Services.Background;

/// <summary>
/// Base for GMS background workers. Each worker runs a bounded cycle on a configurable interval,
/// creating a FRESH scoped DbContext per cycle, never overlapping runs (sequential loop), handling
/// failures without crashing the process, updating a <see cref="WorkerHeartbeat"/>, and emitting
/// duration/error metrics. A public <see cref="RunOnceAsync"/> lets diagnostics/tests run one cycle
/// deterministically (no real polling delay).
/// </summary>
public abstract class BackgroundWorkerBase : BackgroundService
{
    protected readonly IServiceScopeFactory ScopeFactory;
    protected readonly ILogger Logger;
    protected readonly BackgroundProcessingOptions Root;

    protected BackgroundWorkerBase(IServiceScopeFactory scopeFactory, ILogger logger, IOptions<BackgroundProcessingOptions> options)
    {
        ScopeFactory = scopeFactory;
        Logger = logger;
        Root = options.Value;
    }

    /// <summary>Stable worker name (used for heartbeat, metrics tag and run-once lookup).</summary>
    public abstract string WorkerName { get; }
    protected abstract bool WorkerEnabled { get; }
    protected abstract TimeSpan PollInterval { get; }

    /// <summary>Runs one bounded cycle. Returns the number of items processed.</summary>
    protected abstract Task<int> RunCycleAsync(IServiceScope scope, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Root.Enabled || !WorkerEnabled)
        {
            Logger.LogInformation("{Worker} devre dışı (yapılandırma).", WorkerName);
            return;
        }

        Logger.LogInformation("{Worker} başlatıldı (aralık {Interval}s).", WorkerName, PollInterval.TotalSeconds);
        // Small startup grace so the app finishes booting before the first cycle.
        try { await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken); } catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceInternalAsync(stoppingToken);
            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    /// <summary>Diagnostic/test entry point — runs exactly one cycle and returns items processed.</summary>
    public Task<int> RunOnceAsync(CancellationToken ct = default) => RunOnceInternalAsync(ct);

    private async Task<int> RunOnceInternalAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        await WriteHeartbeatAsync(h => h.LastStartedAt = DateTime.UtcNow, ct);
        var processed = 0;
        try
        {
            using var scope = ScopeFactory.CreateScope();
            processed = await RunCycleAsync(scope, ct);
            await WriteHeartbeatAsync(h => h.LastSucceededAt = DateTime.UtcNow, ct);
            if (processed > 0) Logger.LogInformation("{Worker} {Count} öğe işledi.", WorkerName, processed);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            GmsTelemetry.WorkerErrors.Add(1, GmsTelemetry.Worker(WorkerName));
            Logger.LogError(ex, "{Worker} döngüsü hata verdi.", WorkerName);
            await WriteHeartbeatAsync(h => { h.LastFailedAt = DateTime.UtcNow; h.LastError = Truncate(ex.Message, 1000); }, ct);
        }
        finally
        {
            sw.Stop();
            GmsTelemetry.WorkerDurationMs.Record(sw.Elapsed.TotalMilliseconds, GmsTelemetry.Worker(WorkerName));
        }
        return processed;
    }

    /// <summary>Upserts the heartbeat in its own scope so it is isolated from cycle transactions.</summary>
    private async Task WriteHeartbeatAsync(Action<WorkerHeartbeat> mutate, CancellationToken ct)
    {
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GmsDbContext>();
            var hb = await db.WorkerHeartbeats.FirstOrDefaultAsync(h => h.WorkerName == WorkerName && h.InstanceId == Root.InstanceId, ct);
            if (hb is null)
            {
                hb = new WorkerHeartbeat { Id = Guid.NewGuid(), WorkerName = WorkerName, InstanceId = Root.InstanceId };
                db.WorkerHeartbeats.Add(hb);
            }
            mutate(hb);
            hb.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Heartbeat is best-effort — never let it crash a cycle.
            Logger.LogWarning(ex, "{Worker} heartbeat yazılamadı.", WorkerName);
        }
    }

    protected static string? Truncate(string? v, int max) => v is null ? null : (v.Length <= max ? v : v[..max]);
}
