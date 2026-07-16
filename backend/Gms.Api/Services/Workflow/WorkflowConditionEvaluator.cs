using System.Globalization;
using Gms.Api.Common;
using Gms.Api.Domain;

namespace Gms.Api.Services.Workflow;

/// <summary>
/// Safe, limited condition evaluator for workflow transitions. It reads ONLY allowlisted
/// object fields from a pre-built context dictionary and compares them with a fixed set of
/// operators. There is NO dynamic C#, reflection, script execution or expression language —
/// unknown fields/operators are rejected. This is the single place transition routing decisions
/// are made, so it can be reasoned about and validated at publish time.
/// </summary>
public static class WorkflowConditionEvaluator
{
    /// <summary>
    /// Builds the evaluation context (allowlisted fields → string values) from a change request.
    /// The change must have its <see cref="ChangeRequest.Environment"/> loaded for environmentName.
    /// </summary>
    public static IReadOnlyDictionary<string, string> BuildChangeContext(ChangeRequest change, int? readinessScore = null)
    {
        var ctx = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [WorkflowChangeFields.ChangeClass] = change.ChangeClass ?? string.Empty,
            [WorkflowChangeFields.ChangeType] = change.ChangeType ?? string.Empty,
            [WorkflowChangeFields.Priority] = change.Priority ?? string.Empty,
            [WorkflowChangeFields.RiskLevel] = change.RiskLevel ?? string.Empty,
            [WorkflowChangeFields.RiskScore] = change.RiskScore.ToString(CultureInfo.InvariantCulture),
            [WorkflowChangeFields.EnvironmentName] = change.Environment?.Name ?? string.Empty,
            [WorkflowChangeFields.Status] = change.Status ?? string.Empty,
        };
        if (readinessScore.HasValue)
            ctx[WorkflowChangeFields.ReadinessScore] = readinessScore.Value.ToString(CultureInfo.InvariantCulture);
        return ctx;
    }

    /// <summary>
    /// Evaluates a single transition against a context. Always → true (unconditional fallback).
    /// Field-based conditions read the allowlisted field and apply the operator. Missing fields
    /// or unparsable numeric comparisons evaluate to false (never throw at runtime).
    /// </summary>
    public static bool Evaluate(IReadOnlyDictionary<string, string> context, WorkflowTransitionDefinition transition)
    {
        if (transition.ConditionType == WorkflowConditionTypes.Always)
            return true;

        var field = transition.ConditionField;
        if (string.IsNullOrWhiteSpace(field) || !WorkflowChangeFields.All.Contains(field))
            return false; // not allowlisted → never matches

        if (!context.TryGetValue(field, out var actual))
            return false;

        var op = transition.Operator ?? WorkflowOperators.Equals;
        var expected = transition.ExpectedValue ?? string.Empty;

        // Numeric fields use numeric comparison; everything else is string comparison.
        if (WorkflowChangeFields.Numeric.Contains(field))
            return EvaluateNumeric(actual, op, expected);

        return EvaluateString(actual, op, expected);
    }

    private static bool EvaluateString(string actual, string op, string expected) => op switch
    {
        WorkflowOperators.Equals => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
        WorkflowOperators.NotEquals => !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
        WorkflowOperators.Contains => actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
        _ => false // ordering operators are invalid on strings
    };

    private static bool EvaluateNumeric(string actual, string op, string expected)
    {
        if (!decimal.TryParse(actual, NumberStyles.Any, CultureInfo.InvariantCulture, out var a) ||
            !decimal.TryParse(expected, NumberStyles.Any, CultureInfo.InvariantCulture, out var e))
            return false;

        return op switch
        {
            WorkflowOperators.Equals => a == e,
            WorkflowOperators.NotEquals => a != e,
            WorkflowOperators.GreaterThan => a > e,
            WorkflowOperators.GreaterThanOrEqual => a >= e,
            WorkflowOperators.LessThan => a < e,
            WorkflowOperators.LessThanOrEqual => a <= e,
            WorkflowOperators.Contains => false, // meaningless for numbers
            _ => false
        };
    }
}
