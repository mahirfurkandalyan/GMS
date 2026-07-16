using Gms.Api.Common;

namespace Gms.Api.Services;

/// <summary>Inputs required to score a change request's risk.</summary>
public sealed record ChangeRiskInput(
    string EnvironmentName,
    string ChangeClass,
    string ChangeType,
    bool HasCriticalAsset,
    bool HasRollbackScript,
    bool HasBusinessReason);

public sealed record ChangeRiskResult(int Score, string Level);

/// <summary>
/// Server-side risk model. Risk is NEVER chosen manually and NEVER computed on
/// the frontend alone — this service is the single authority. Rules are additive
/// weights; the resulting score maps to a level band.
/// </summary>
public class ChangeRiskService
{
    public ChangeRiskResult Calculate(ChangeRiskInput input)
    {
        var score = 0;

        // Environment weight.
        if (string.Equals(input.EnvironmentName, "PROD", StringComparison.OrdinalIgnoreCase))
        {
            score += 30;
        }

        // Change class weight.
        if (input.ChangeClass == ChangeClasses.Emergency)
        {
            score += 30;
        }

        // Change type weight.
        score += input.ChangeType switch
        {
            ChangeTypes.DatabaseSchemaChange => 25,
            ChangeTypes.SqlDataFix => 25,
            ChangeTypes.StoredProcedureFunctionChange => 20,
            ChangeTypes.ApplicationDeployment => 15,
            _ => 0
        };

        // Any critical affected asset.
        if (input.HasCriticalAsset)
        {
            score += 25;
        }

        // Missing rollback script raises risk.
        if (!input.HasRollbackScript)
        {
            score += 20;
        }

        // Missing business reason raises risk.
        if (!input.HasBusinessReason)
        {
            score += 15;
        }

        return new ChangeRiskResult(score, ToLevel(score));
    }

    private static string ToLevel(int score) => score switch
    {
        >= 80 => ChangeRiskLevels.Critical,
        >= 60 => ChangeRiskLevels.High,
        >= 30 => ChangeRiskLevels.Medium,
        _ => ChangeRiskLevels.Low
    };
}
