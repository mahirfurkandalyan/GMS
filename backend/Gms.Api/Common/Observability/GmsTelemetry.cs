using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Gms.Api.Common.Observability;

/// <summary>
/// Central telemetry primitives built on the .NET BCL (no vendor lock-in): a single
/// <see cref="ActivitySource"/> for custom spans and a single <see cref="Meter"/> with the
/// application metrics. OpenTelemetry (when configured) subscribes to these by name. Tags are
/// deliberately low-cardinality (module/provider/result/channel) — never user/object/correlation ids.
/// </summary>
public static class GmsTelemetry
{
    public const string SourceName = "Gms.Api";
    public const string MeterName = "Gms.Api";

    /// <summary>Custom activity source for workflow/integration/notification/validation/deployment spans.</summary>
    public static readonly ActivitySource ActivitySource = new(SourceName);

    private static readonly Meter Meter = new(MeterName);

    // ── Integration metrics ──
    public static readonly Counter<long> IntegrationExecutions =
        Meter.CreateCounter<long>("gms_integration_executions_total", description: "Toplam entegrasyon yürütmesi.");
    public static readonly Counter<long> IntegrationFailures =
        Meter.CreateCounter<long>("gms_integration_failures_total", description: "Başarısız entegrasyon yürütmeleri.");
    public static readonly Counter<long> IntegrationDeadLetters =
        Meter.CreateCounter<long>("gms_integration_deadletters_total", description: "Ölü mektuba alınan yürütmeler.");

    // ── Notification metrics ──
    public static readonly Counter<long> NotificationDeliveries =
        Meter.CreateCounter<long>("gms_notification_deliveries_total", description: "İşlenen bildirim teslimatları.");
    public static readonly Counter<long> NotificationDeliveryFailures =
        Meter.CreateCounter<long>("gms_notification_delivery_failures_total", description: "Başarısız bildirim teslimatları.");

    // ── Background worker metrics ──
    public static readonly Histogram<double> WorkerDurationMs =
        Meter.CreateHistogram<double>("gms_background_worker_duration_ms", unit: "ms", description: "Worker döngü süresi.");
    public static readonly Counter<long> WorkerErrors =
        Meter.CreateCounter<long>("gms_background_worker_errors_total", description: "Worker döngü hataları.");

    // ── HTTP metric (worker-independent app-level timing; ASP.NET instrumentation also emits its own) ──
    public static readonly Histogram<double> HttpRequestDurationMs =
        Meter.CreateHistogram<double>("gms_http_request_duration_ms", unit: "ms", description: "HTTP istek süresi.");

    // ── Workflow overdue gauge (updated by the SLA worker each cycle; low-cardinality) ──
    private static int _overdueTasks;
    private static readonly ObservableGauge<int> OverdueGauge =
        Meter.CreateObservableGauge("gms_workflow_tasks_overdue", () => Volatile.Read(ref _overdueTasks), description: "Gecikmiş workflow görevleri.");

    /// <summary>Publishes the current overdue-task count for the observable gauge.</summary>
    public static void SetOverdueTasks(int count) => Volatile.Write(ref _overdueTasks, count);

    // ── low-cardinality tag helpers ──
    public static KeyValuePair<string, object?> Module(string value) => new("module", value);
    public static KeyValuePair<string, object?> Provider(string value) => new("provider", value);
    public static KeyValuePair<string, object?> Result(string value) => new("result", value);
    public static KeyValuePair<string, object?> Channel(string value) => new("channel", value);
    public static KeyValuePair<string, object?> Worker(string value) => new("worker", value);
}
