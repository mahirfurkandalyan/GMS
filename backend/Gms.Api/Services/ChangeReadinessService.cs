using Gms.Api.Common;

namespace Gms.Api.Services;

/// <summary>Inputs required to evaluate a change request's readiness.</summary>
public sealed record ChangeReadinessInput(
    bool HasBusinessReason,
    bool HasEnvironment,
    int AssetCount,
    string ChangeType,
    bool HasRollbackScript,
    int DocumentCount,
    bool HasPlannedDate);

public sealed record ChangeReadinessFinding(
    string Code,
    string Severity,
    string Message,
    string Recommendation);

public sealed record ChangeReadinessResult(
    int ReadinessScore,
    IReadOnlyList<ChangeReadinessFinding> Findings);

/// <summary>
/// Server-side pre-check / readiness evaluation. Returns findings and a score;
/// findings are NOT persisted (returned from the detail endpoint and used to gate
/// submission). No backend validation engine yet — plain, explainable rules.
/// </summary>
public class ChangeReadinessService
{
    public const string SeverityCritical = "Critical";
    public const string SeverityWarning = "Warning";

    public ChangeReadinessResult Evaluate(ChangeReadinessInput input)
    {
        // Each check is (passed, finding-if-failed). Score = passed / total.
        var checks = new List<(bool Passed, ChangeReadinessFinding? Finding)>
        {
            Check(input.HasBusinessReason,
                "BUSINESS_REASON_MISSING", SeverityCritical,
                "İş gerekçesi girilmemiş.",
                "Değişikliğin neden gerekli olduğunu açıklayan iş gerekçesini ekleyin."),

            Check(input.HasEnvironment,
                "ENVIRONMENT_MISSING", SeverityCritical,
                "Hedef ortam seçilmemiş.",
                "Değişikliğin uygulanacağı ortamı seçin."),

            Check(input.AssetCount > 0,
                "NO_AFFECTED_ASSET", SeverityWarning,
                "Etkilenen varlık eklenmemiş.",
                "Bu değişikliğin etkilediği en az bir varlığı ekleyin."),

            Check(!ChangeTypes.SqlRelated.Contains(input.ChangeType) || input.HasRollbackScript,
                "ROLLBACK_MISSING", SeverityCritical,
                "SQL/veritabanı değişikliği için geri alma betiği tanımlanmamış.",
                "İlgili revizyona geri alma (rollback) betiğini ekleyin."),

            Check(input.DocumentCount > 0,
                "NO_DOCUMENT", SeverityWarning,
                "Destekleyici doküman eklenmemiş.",
                "En az bir destekleyici doküman (SQL betiği, test kanıtı vb.) ekleyin."),

            Check(input.HasPlannedDate,
                "PLANNED_DATE_MISSING", SeverityWarning,
                "Planlanan uygulama tarihi girilmemiş.",
                "Değişikliğin planlanan uygulama tarihini belirtin.")
        };

        var total = checks.Count;
        var passed = checks.Count(c => c.Passed);
        var score = total == 0 ? 100 : (int)Math.Round(passed * 100.0 / total);
        var findings = checks.Where(c => !c.Passed).Select(c => c.Finding!).ToList();

        return new ChangeReadinessResult(score, findings);
    }

    private static (bool, ChangeReadinessFinding?) Check(
        bool passed, string code, string severity, string message, string recommendation) =>
        passed ? (true, null) : (false, new ChangeReadinessFinding(code, severity, message, recommendation));
}
