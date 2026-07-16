using Gms.Api.Common.Observability;
using Gms.Api.Services.Integrations;
using Microsoft.Extensions.Options;

namespace Gms.Api.Services.Background;

/// <summary>
/// Drains the IntegrationExecution outbox: claims a bounded batch of Pending / retry-due executions
/// (lease + RowVersion, no double-processing), then dispatches each through the existing
/// <see cref="IIntegrationDispatcher"/> (attempt recorded, retry/dead-letter applied).
/// </summary>
public sealed class IntegrationDispatchWorker : BackgroundWorkerBase
{
    private readonly WorkerOptions _opts;

    public IntegrationDispatchWorker(IServiceScopeFactory scopeFactory, ILogger<IntegrationDispatchWorker> logger,
        IOptions<BackgroundProcessingOptions> options) : base(scopeFactory, logger, options)
        => _opts = options.Value.IntegrationDispatch;

    public override string WorkerName => "IntegrationDispatch";
    protected override bool WorkerEnabled => _opts.Enabled;
    protected override TimeSpan PollInterval => _opts.PollInterval;

    protected override async Task<int> RunCycleAsync(IServiceScope scope, CancellationToken ct)
    {
        using var activity = GmsTelemetry.ActivitySource.StartActivity("integration.dispatch.cycle");
        var dispatcher = scope.ServiceProvider.GetRequiredService<IIntegrationDispatcher>();
        var claimed = await dispatcher.ClaimDispatchableAsync(Root.InstanceId, _opts.Lease, _opts.Batch, ct);
        foreach (var id in claimed)
        {
            if (ct.IsCancellationRequested) break;
            await dispatcher.DispatchOneAsync(id, null, ct);
        }
        activity?.SetTag("gms.claimed", claimed.Count);
        return claimed.Count;
    }
}
