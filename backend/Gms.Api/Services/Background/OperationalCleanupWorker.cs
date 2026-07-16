using Gms.Api.Common;
using Gms.Api.Common.Observability;
using Gms.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Gms.Api.Services.Background;

/// <summary>
/// DISABLED-BY-DEFAULT cleanup foundation. For this sprint it only REPORTS/COUNTS candidate rows —
/// it deletes NOTHING. Audit records are never deleted and documents are never purged here. A
/// future sprint can add explicit, retention-config-driven deletion behind the same worker.
/// </summary>
public sealed class OperationalCleanupWorker : BackgroundWorkerBase
{
    private readonly WorkerOptions _opts;

    public OperationalCleanupWorker(IServiceScopeFactory scopeFactory, ILogger<OperationalCleanupWorker> logger,
        IOptions<BackgroundProcessingOptions> options) : base(scopeFactory, logger, options)
        => _opts = options.Value.OperationalCleanup;

    public override string WorkerName => "OperationalCleanup";
    protected override bool WorkerEnabled => _opts.Enabled;
    protected override TimeSpan PollInterval => _opts.PollInterval;

    protected override async Task<int> RunCycleAsync(IServiceScope scope, CancellationToken ct)
    {
        using var activity = GmsTelemetry.ActivitySource.StartActivity("operational.cleanup.report");
        var db = scope.ServiceProvider.GetRequiredService<GmsDbContext>();
        var candidates = await CountCandidatesAsync(db, ct);

        // Report only — no deletion in this sprint.
        Logger.LogInformation(
            "Temizlik adayları (silinmez): expiredRefreshTokens={A}, oldSentDeliveries={B}, oldIntegrationAttempts={C}, softDeletedDocuments={D}, oldSecurityAudit={E}",
            candidates.ExpiredRefreshTokens, candidates.OldSentDeliveries, candidates.OldIntegrationAttempts,
            candidates.SoftDeletedDocuments, candidates.OldSecurityAudit);
        activity?.SetTag("gms.cleanup_candidates_total", candidates.Total);
        return 0; // nothing deleted
    }

    /// <summary>Counts cleanup candidates (read-only) — reused by operational status if needed.</summary>
    public static async Task<CleanupCandidates> CountCandidatesAsync(GmsDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);
        var ninetyDaysAgo = now.AddDays(-90);

        return new CleanupCandidates
        {
            ExpiredRefreshTokens = await db.RefreshTokens.AsNoTracking()
                .CountAsync(t => t.ExpiresAt < now && t.RevokedAt == null, ct),
            OldSentDeliveries = await db.NotificationDeliveries.AsNoTracking()
                .CountAsync(d => (d.Status == NotificationDeliveryStatuses.Sent || d.Status == NotificationDeliveryStatuses.Delivered)
                    && d.SentAt != null && d.SentAt < thirtyDaysAgo, ct),
            OldIntegrationAttempts = await db.IntegrationExecutionAttempts.AsNoTracking()
                .CountAsync(a => a.CreatedAt < thirtyDaysAgo, ct),
            SoftDeletedDocuments = await db.Documents.AsNoTracking()
                .CountAsync(d => d.Status == DocumentStatuses.Deleted, ct),
            // Reported for visibility only — audit records are NEVER deleted by this worker.
            OldSecurityAudit = await db.SecurityAuditEvents.AsNoTracking()
                .CountAsync(e => e.CreatedAt < ninetyDaysAgo, ct)
        };
    }
}

/// <summary>Read-only count of cleanup candidates.</summary>
public sealed class CleanupCandidates
{
    public int ExpiredRefreshTokens { get; set; }
    public int OldSentDeliveries { get; set; }
    public int OldIntegrationAttempts { get; set; }
    public int SoftDeletedDocuments { get; set; }
    public int OldSecurityAudit { get; set; }
    public int Total => ExpiredRefreshTokens + OldSentDeliveries + OldIntegrationAttempts + SoftDeletedDocuments + OldSecurityAudit;
}
