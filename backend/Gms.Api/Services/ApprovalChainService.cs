using Gms.Api.Common;

namespace Gms.Api.Services;

/// <summary>Definition of a single approval step (role-based, no DB access).</summary>
public sealed record ApprovalStepDefinition(int StepNo, string StepName, string ApproverRole);

/// <summary>
/// Builds the approval chain for a change based on its risk level. Pure logic —
/// user resolution happens later in ApprovalService. Chain grows with risk.
/// </summary>
public class ApprovalChainService
{
    public IReadOnlyList<ApprovalStepDefinition> BuildForChange(string riskLevel)
    {
        var roles = riskLevel switch
        {
            ChangeRiskLevels.Critical => new[] { ApproverRoles.Architect, ApproverRoles.QA, ApproverRoles.ReleaseManager, ApproverRoles.Admin },
            ChangeRiskLevels.High => new[] { ApproverRoles.Architect, ApproverRoles.QA, ApproverRoles.ReleaseManager },
            ChangeRiskLevels.Medium => new[] { ApproverRoles.Architect, ApproverRoles.QA },
            _ => new[] { ApproverRoles.Architect } // Low (and any unknown) → single Architect step
        };

        var steps = new List<ApprovalStepDefinition>();
        for (var i = 0; i < roles.Length; i++)
        {
            steps.Add(new ApprovalStepDefinition(i + 1, StepName(roles[i]), roles[i]));
        }
        return steps;
    }

    private static string StepName(string role) => role switch
    {
        ApproverRoles.Architect => "Mimari Onayı",
        ApproverRoles.QA => "Kalite (QA) Onayı",
        ApproverRoles.ReleaseManager => "Yayın Yöneticisi Onayı",
        ApproverRoles.Admin => "Yönetici Onayı",
        _ => $"{role} Onayı"
    };
}
