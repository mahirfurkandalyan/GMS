namespace Gms.Api.Domain;

/// <summary>
/// Liveness/health record for a background worker on a given node. Updated each cycle so
/// operational status and health checks can detect stale/failed workers. One row per
/// (WorkerName, InstanceId).
/// </summary>
public class WorkerHeartbeat
{
    public Guid Id { get; set; }

    public string WorkerName { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;

    public DateTime? LastStartedAt { get; set; }
    public DateTime? LastSucceededAt { get; set; }
    public DateTime? LastFailedAt { get; set; }
    public string? LastError { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
