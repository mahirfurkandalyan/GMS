namespace Gms.Api.Domain;

/// <summary>The deployment plan for a release (1:1 with ReleasePlan).</summary>
public class ReleaseDeploymentPlan
{
    public Guid Id { get; set; }

    public Guid ReleasePlanId { get; set; }
    public ReleasePlan? ReleasePlan { get; set; }

    public string DeploymentStrategy { get; set; } = string.Empty;
    public string CommunicationPlan { get; set; } = string.Empty;
    public string RollbackStrategy { get; set; } = string.Empty;

    public bool DowntimeExpected { get; set; }
    public int EstimatedDowntimeMinutes { get; set; }

    public string Notes { get; set; } = string.Empty;
}
