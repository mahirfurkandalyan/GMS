namespace Gms.Api.Contracts;

/// <summary>Operational status snapshot (backlog counts + worker heartbeats).</summary>
public class OperationalStatusDto
{
    public int PendingIntegrationExecutions { get; set; }
    public int RetryScheduledIntegrationExecutions { get; set; }
    public int DeadLetterIntegrationExecutions { get; set; }
    public int PendingNotificationDeliveries { get; set; }
    public int FailedNotificationDeliveries { get; set; }
    public int OverdueWorkflowTasks { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<WorkerHeartbeatDto> Workers { get; set; } = new();
}

public class WorkerHeartbeatDto
{
    public string WorkerName { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public DateTime? LastStartedAt { get; set; }
    public DateTime? LastSucceededAt { get; set; }
    public DateTime? LastFailedAt { get; set; }
    public string? LastError { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Result of a manual run-once diagnostic invocation.</summary>
public class WorkerRunResultDto
{
    public string WorkerName { get; set; } = string.Empty;
    public int Processed { get; set; }
}
