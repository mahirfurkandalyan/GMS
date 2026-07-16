using Gms.Api.Common.Observability;
using Microsoft.Extensions.Options;

namespace Gms.Api.Services.Background;

/// <summary>
/// Processes pending Email notification deliveries asynchronously (after their notification
/// transaction has committed). InApp is delivered immediately at creation; Email is sent here via
/// <see cref="NotificationDeliveryDispatcher"/> with retry/backoff and dead-lettering.
/// </summary>
public sealed class NotificationDeliveryWorker : BackgroundWorkerBase
{
    private readonly WorkerOptions _opts;

    public NotificationDeliveryWorker(IServiceScopeFactory scopeFactory, ILogger<NotificationDeliveryWorker> logger,
        IOptions<BackgroundProcessingOptions> options) : base(scopeFactory, logger, options)
        => _opts = options.Value.NotificationDelivery;

    public override string WorkerName => "NotificationDelivery";
    protected override bool WorkerEnabled => _opts.Enabled;
    protected override TimeSpan PollInterval => _opts.PollInterval;

    protected override async Task<int> RunCycleAsync(IServiceScope scope, CancellationToken ct)
    {
        using var activity = GmsTelemetry.ActivitySource.StartActivity("notification.delivery.cycle");
        var dispatcher = scope.ServiceProvider.GetRequiredService<NotificationDeliveryDispatcher>();
        var claimed = await dispatcher.ClaimAsync(Root.InstanceId, _opts.Lease, _opts.Batch, ct);
        foreach (var id in claimed)
        {
            if (ct.IsCancellationRequested) break;
            await dispatcher.ProcessAsync(id, _opts.MaxRetryCount, ct);
        }
        activity?.SetTag("gms.claimed", claimed.Count);
        return claimed.Count;
    }
}
