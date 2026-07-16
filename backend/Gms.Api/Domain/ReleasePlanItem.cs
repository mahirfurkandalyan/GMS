namespace Gms.Api.Domain;

/// <summary>
/// A change included in a release plan (ordered). Links a ReleasePlan to an
/// approved ChangeRequest — the only allowed relationship (no draft/rejected).
/// </summary>
public class ReleasePlanItem
{
    public Guid Id { get; set; }

    public Guid ReleasePlanId { get; set; }
    public ReleasePlan? ReleasePlan { get; set; }

    public Guid ChangeRequestId { get; set; }
    public ChangeRequest? ChangeRequest { get; set; }

    public int DeploymentOrder { get; set; }
    public int EstimatedMinutes { get; set; }
    public bool RollbackRequired { get; set; }
}
