using Gms.Api.Data;
using Gms.Api.Services.Background;
using Gms.Api.Services.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Gms.Api.Common.Health;

/// <summary>Readiness: SQL Server reachable.</summary>
public sealed class SqlServerHealthCheck : IHealthCheck
{
    private readonly GmsDbContext _db;
    public SqlServerHealthCheck(GmsDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            return await _db.Database.CanConnectAsync(ct)
                ? HealthCheckResult.Healthy("Veritabanına erişilebiliyor.")
                : HealthCheckResult.Unhealthy("Veritabanına erişilemiyor.");
        }
        catch
        {
            // Never leak the exception/stack trace to the response.
            return HealthCheckResult.Unhealthy("Veritabanı bağlantı hatası.");
        }
    }
}

/// <summary>Readiness: Data Protection can protect + unprotect (key ring usable).</summary>
public sealed class DataProtectionHealthCheck : IHealthCheck
{
    private readonly ISecretProtector _secrets;
    public DataProtectionHealthCheck(ISecretProtector secrets) => _secrets = secrets;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            const string probe = "gms-health-probe";
            var ok = _secrets.Unprotect(_secrets.Protect(probe)) == probe;
            return Task.FromResult(ok
                ? HealthCheckResult.Healthy("Data Protection çalışıyor.")
                : HealthCheckResult.Unhealthy("Data Protection doğrulaması başarısız."));
        }
        catch
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Data Protection hatası."));
        }
    }
}

/// <summary>Readiness: document storage root is writable/readable.</summary>
public sealed class StorageHealthCheck : IHealthCheck
{
    private readonly string _root;
    public StorageHealthCheck(IWebHostEnvironment env, IConfiguration config)
    {
        var configured = config["Storage:DocumentsRoot"];
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(env.ContentRootPath, "storage", "documents")
            : configured;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(_root);
            var probe = Path.Combine(_root, $".health-{Guid.NewGuid():N}.tmp");
            await File.WriteAllTextAsync(probe, "ok", ct);
            var read = await File.ReadAllTextAsync(probe, ct);
            File.Delete(probe);
            return read == "ok"
                ? HealthCheckResult.Healthy("Doküman deposu yazılabilir.")
                : HealthCheckResult.Unhealthy("Doküman deposu okuma/yazma uyuşmazlığı.");
        }
        catch
        {
            return HealthCheckResult.Unhealthy("Doküman deposu erişilemiyor.");
        }
    }
}

/// <summary>Readiness: enabled workers have reported success recently (freshness).</summary>
public sealed class WorkerFreshnessHealthCheck : IHealthCheck
{
    private readonly GmsDbContext _db;
    private readonly HealthOptions _health;
    private readonly BackgroundProcessingOptions _bg;

    public WorkerFreshnessHealthCheck(GmsDbContext db, IOptions<HealthOptions> health, IOptions<BackgroundProcessingOptions> bg)
    {
        _db = db;
        _health = health.Value;
        _bg = bg.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        // If background processing is off, freshness is not applicable.
        if (!_bg.Enabled) return HealthCheckResult.Healthy("Arka plan işleme kapalı.");

        var staleBefore = DateTime.UtcNow.AddMinutes(-_health.WorkerStaleMinutes);
        var expected = new List<string>();
        if (_bg.IntegrationDispatch.Enabled) expected.Add("IntegrationDispatch");
        if (_bg.NotificationDelivery.Enabled) expected.Add("NotificationDelivery");
        if (_bg.WorkflowSla.Enabled) expected.Add("WorkflowSla");
        if (expected.Count == 0) return HealthCheckResult.Healthy("Etkin worker yok.");

        var beats = await _db.WorkerHeartbeats.AsNoTracking()
            .Where(h => expected.Contains(h.WorkerName))
            .ToListAsync(ct);

        var stale = expected.Where(name =>
        {
            var beat = beats.FirstOrDefault(b => b.WorkerName == name);
            return beat?.LastSucceededAt == null || beat.LastSucceededAt < staleBefore;
        }).ToList();

        // Degraded (not unhealthy): workers may simply not have run yet after a fresh start.
        return stale.Count == 0
            ? HealthCheckResult.Healthy("Worker'lar taze.")
            : HealthCheckResult.Degraded($"Bayat/başlamamış worker sayısı: {stale.Count}.");
    }
}

/// <summary>Readiness: backlogs are within configured thresholds.</summary>
public sealed class BacklogHealthCheck : IHealthCheck
{
    private readonly IOperationalStatusService _status;
    private readonly HealthOptions _health;

    public BacklogHealthCheck(IOperationalStatusService status, IOptions<HealthOptions> health)
    {
        _status = status;
        _health = health.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var integration = await _status.IntegrationBacklogAsync(ct);
        var notification = await _status.NotificationBacklogAsync(ct);
        if (integration > _health.IntegrationPendingWarningThreshold || notification > _health.NotificationPendingWarningThreshold)
            return HealthCheckResult.Degraded($"Birikim eşik üstünde (entegrasyon={integration}, bildirim={notification}).");
        return HealthCheckResult.Healthy($"Birikim normal (entegrasyon={integration}, bildirim={notification}).");
    }
}
