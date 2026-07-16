namespace Gms.Api.Common;

public sealed record WorkflowStepSeed(string StepKey, string Name, string StepType, int StepOrder,
    string? AssignedRole = null, bool IsRequired = true, int? DueInHours = null);

public sealed record WorkflowTransitionSeed(string FromStepKey, string ToStepKey, string ConditionType,
    int Priority, string? ConditionField = null, string? Operator = null, string? ExpectedValue = null);

public sealed record WorkflowSeed(string Code, string Name, string Description, string Category,
    string TriggerObjectType, string TriggerEvent, string ChangeClass,
    IReadOnlyList<WorkflowStepSeed> Steps, IReadOnlyList<WorkflowTransitionSeed> Transitions);

/// <summary>
/// Deterministic catalog of the seeded governance workflows (each seeded as a definition +
/// one Published v1). Change submissions resolve a workflow by ChangeClass.
/// </summary>
public static class WorkflowCatalog
{
    private const int Due = 48;

    public static readonly WorkflowSeed Standard = new(
        "CHANGE_STANDARD_DEFAULT", "Standart Değişiklik Akışı", "Düşük riskli standart değişiklikler için kısa onay akışı.",
        WorkflowCategories.ChangeManagement, WorkflowTriggers.ChangeRequestObject, WorkflowTriggers.ChangeSubmittedEvent, "Standard",
        new[]
        {
            new WorkflowStepSeed("START", "Başlangıç", WorkflowStepTypes.Start, 1),
            new WorkflowStepSeed("ARCH", "Mimari Onayı", WorkflowStepTypes.Approval, 2, SystemRoles.Architect, true, Due),
            new WorkflowStepSeed("END", "Bitiş", WorkflowStepTypes.End, 3),
        },
        new[]
        {
            new WorkflowTransitionSeed("START", "ARCH", WorkflowConditionTypes.Always, 1),
            new WorkflowTransitionSeed("ARCH", "END", WorkflowConditionTypes.Always, 1),
        });

    public static readonly WorkflowSeed Normal = new(
        "CHANGE_NORMAL_DEFAULT", "Normal Değişiklik Akışı", "Normal değişiklikler; yüksek/kritik risk için Yayın Yöneticisi onayı eklenir.",
        WorkflowCategories.ChangeManagement, WorkflowTriggers.ChangeRequestObject, WorkflowTriggers.ChangeSubmittedEvent, "Normal",
        new[]
        {
            new WorkflowStepSeed("START", "Başlangıç", WorkflowStepTypes.Start, 1),
            new WorkflowStepSeed("ARCH", "Mimari Onayı", WorkflowStepTypes.Approval, 2, SystemRoles.Architect, true, Due),
            new WorkflowStepSeed("QA", "Kalite (QA) Onayı", WorkflowStepTypes.Approval, 3, SystemRoles.QA, true, Due),
            new WorkflowStepSeed("RISK", "Risk Değerlendirmesi", WorkflowStepTypes.Condition, 4),
            new WorkflowStepSeed("RM", "Yayın Yöneticisi Onayı", WorkflowStepTypes.Approval, 5, SystemRoles.ReleaseManager, true, Due),
            new WorkflowStepSeed("END", "Bitiş", WorkflowStepTypes.End, 6),
        },
        new[]
        {
            new WorkflowTransitionSeed("START", "ARCH", WorkflowConditionTypes.Always, 1),
            new WorkflowTransitionSeed("ARCH", "QA", WorkflowConditionTypes.Always, 1),
            new WorkflowTransitionSeed("QA", "RISK", WorkflowConditionTypes.Always, 1),
            // High or Critical risk → Release Manager approval (first matching by priority wins).
            new WorkflowTransitionSeed("RISK", "RM", WorkflowConditionTypes.RiskLevel, 1, WorkflowChangeFields.RiskLevel, WorkflowOperators.Equals, "Critical"),
            new WorkflowTransitionSeed("RISK", "RM", WorkflowConditionTypes.RiskLevel, 2, WorkflowChangeFields.RiskLevel, WorkflowOperators.Equals, "High"),
            new WorkflowTransitionSeed("RISK", "END", WorkflowConditionTypes.Always, 3),
            new WorkflowTransitionSeed("RM", "END", WorkflowConditionTypes.Always, 1),
        });

    public static readonly WorkflowSeed Emergency = new(
        "CHANGE_EMERGENCY_DEFAULT", "Acil Değişiklik Akışı", "Acil değişiklikler; Mimari + Yayın Yöneticisi + Admin onayı.",
        WorkflowCategories.ChangeManagement, WorkflowTriggers.ChangeRequestObject, WorkflowTriggers.ChangeSubmittedEvent, "Emergency",
        new[]
        {
            new WorkflowStepSeed("START", "Başlangıç", WorkflowStepTypes.Start, 1),
            new WorkflowStepSeed("ARCH", "Mimari Onayı", WorkflowStepTypes.Approval, 2, SystemRoles.Architect, true, Due),
            new WorkflowStepSeed("RM", "Yayın Yöneticisi Onayı", WorkflowStepTypes.Approval, 3, SystemRoles.ReleaseManager, true, Due),
            new WorkflowStepSeed("ADMIN", "Admin Onayı", WorkflowStepTypes.Approval, 4, SystemRoles.Admin, true, Due),
            new WorkflowStepSeed("END", "Bitiş", WorkflowStepTypes.End, 5),
        },
        new[]
        {
            new WorkflowTransitionSeed("START", "ARCH", WorkflowConditionTypes.Always, 1),
            new WorkflowTransitionSeed("ARCH", "RM", WorkflowConditionTypes.Always, 1),
            new WorkflowTransitionSeed("RM", "ADMIN", WorkflowConditionTypes.Always, 1),
            new WorkflowTransitionSeed("ADMIN", "END", WorkflowConditionTypes.Always, 1),
        });

    public static readonly IReadOnlyList<WorkflowSeed> All = new[] { Standard, Normal, Emergency };

    /// <summary>Resolves the seeded workflow code for a change class (fallback: Normal).</summary>
    public static string CodeForChangeClass(string changeClass) => changeClass switch
    {
        "Standard" => Standard.Code,
        "Emergency" => Emergency.Code,
        _ => Normal.Code
    };
}
