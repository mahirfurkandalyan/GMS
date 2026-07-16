using Gms.Api.Common;

namespace Gms.Api.Services;

/// <summary>Per-change inputs the release risk model needs.</summary>
public sealed record ReleaseRiskChangeInput(string RiskLevel, string ChangeClass);

public sealed record ReleaseRiskInput(string EnvironmentName, IReadOnlyList<ReleaseRiskChangeInput> Changes);

public sealed record ReleaseRiskResult(int Score, string Level);

/// <summary>
/// Server-side release risk model. Combines the average change risk with release
/// size, emergency change count and production targeting. Server is the single
/// authority — the frontend never decides release risk.
/// </summary>
public class ReleaseRiskService
{
    public ReleaseRiskResult Calculate(ReleaseRiskInput input)
    {
        if (input.Changes.Count == 0)
        {
            return new ReleaseRiskResult(0, ReleaseRiskLevels.Low);
        }

        // Average change-risk points.
        var avg = input.Changes.Average(c => RiskPoints(c.RiskLevel));

        // Production environment weight.
        var envWeight = string.Equals(input.EnvironmentName, "PROD", StringComparison.OrdinalIgnoreCase) ? 20 : 0;

        // Emergency changes weight (capped).
        var emergencyCount = input.Changes.Count(c => c.ChangeClass == ChangeClasses.Emergency);
        var emergencyWeight = Math.Min(30, emergencyCount * 10);

        // Release size weight.
        var countWeight = input.Changes.Count switch
        {
            >= 8 => 15,
            >= 5 => 10,
            >= 3 => 5,
            _ => 0
        };

        var score = (int)Math.Round(avg + envWeight + emergencyWeight + countWeight);
        return new ReleaseRiskResult(score, ToLevel(score));
    }

    private static int RiskPoints(string riskLevel) => riskLevel switch
    {
        ChangeRiskLevels.Critical => 80,
        ChangeRiskLevels.High => 55,
        ChangeRiskLevels.Medium => 30,
        _ => 10
    };

    private static string ToLevel(int score) => score switch
    {
        >= 80 => ReleaseRiskLevels.Critical,
        >= 60 => ReleaseRiskLevels.High,
        >= 35 => ReleaseRiskLevels.Medium,
        _ => ReleaseRiskLevels.Low
    };
}
