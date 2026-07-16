namespace Gms.Api.Services.Background;

/// <summary>
/// Background processing configuration. Each worker can be disabled independently, has a
/// configurable poll interval and a bounded batch size, and never loads all pending rows.
/// Secure defaults: master switch on, but intervals conservative.
/// </summary>
public sealed class BackgroundProcessingOptions
{
    public const string SectionName = "BackgroundProcessing";

    /// <summary>Master switch — when false, no worker runs regardless of per-worker flags.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Node identity used for lease ownership (LockedBy) and heartbeats.</summary>
    public string InstanceId { get; set; } = Environment.MachineName;

    /// <summary>Base delay (seconds) for the exponential retry backoff. Production default 30s.</summary>
    public int RetryBaseDelaySeconds { get; set; } = 30;

    public WorkerOptions IntegrationDispatch { get; set; } = new() { PollIntervalSeconds = 15, BatchSize = 25, LeaseSeconds = 60 };
    public WorkerOptions NotificationDelivery { get; set; } = new() { PollIntervalSeconds = 20, BatchSize = 25, LeaseSeconds = 60, MaxRetryCount = 3 };
    public WorkflowSlaOptions WorkflowSla { get; set; } = new();
    public WorkerOptions OperationalCleanup { get; set; } = new() { Enabled = false, PollIntervalSeconds = 3600, BatchSize = 100 };
}

/// <summary>Common per-worker options.</summary>
public sealed class WorkerOptions
{
    public bool Enabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 15;
    public int BatchSize { get; set; } = 25;
    /// <summary>How long a claimed row stays leased before another worker may reclaim it.</summary>
    public int LeaseSeconds { get; set; } = 60;
    public int MaxRetryCount { get; set; } = 3;

    public TimeSpan PollInterval => TimeSpan.FromSeconds(Math.Clamp(PollIntervalSeconds, 1, 3600));
    public TimeSpan Lease => TimeSpan.FromSeconds(Math.Clamp(LeaseSeconds, 5, 3600));
    public int Batch => Math.Clamp(BatchSize, 1, 500);
}

/// <summary>Workflow SLA reminder worker options.</summary>
public sealed class WorkflowSlaOptions
{
    public bool Enabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 60;
    public int BatchSize { get; set; } = 50;
    public int DueSoonHours { get; set; } = 24;
    public int ReminderCooldownHours { get; set; } = 12;

    public TimeSpan PollInterval => TimeSpan.FromSeconds(Math.Clamp(PollIntervalSeconds, 1, 3600));
    public int Batch => Math.Clamp(BatchSize, 1, 500);
}

/// <summary>Observability (logging/tracing/metrics) configuration.</summary>
public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public string ServiceName { get; set; } = "gms-api";
    public bool EnableConsoleExporter { get; set; }
    /// <summary>OTLP collector endpoint (null → OTLP export disabled; no hardcoded default).</summary>
    public string? OtlpEndpoint { get; set; }
    public bool EnableEfInstrumentation { get; set; }
}

/// <summary>Health check thresholds.</summary>
public sealed class HealthOptions
{
    public const string SectionName = "Health";

    public int IntegrationPendingWarningThreshold { get; set; } = 500;
    public int NotificationPendingWarningThreshold { get; set; } = 500;
    /// <summary>A worker is considered stale (unhealthy) if it hasn't succeeded within this window.</summary>
    public int WorkerStaleMinutes { get; set; } = 15;
}
