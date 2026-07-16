using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Gms.Api.Common.Observability;

/// <summary>
/// Ensures every request carries a correlation id. Accepts a valid inbound
/// <c>X-Correlation-Id</c>, otherwise generates one; echoes it in the response header; stores it
/// in <c>HttpContext.Items</c> and a log scope (so all request logs include it) and on the current
/// trace Activity. Correlation id is intentionally NOT used as a metric tag (high cardinality).
/// </summary>
public sealed partial class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var correlationId = ResolveIncoming(context) ?? Guid.NewGuid().ToString("N");
        context.Items[ItemKey] = correlationId;

        // Surface on the response and the current trace.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });
        Activity.Current?.SetTag("gms.correlation_id", correlationId);

        var userId = context.User?.FindFirst(GmsClaimTypes.UserId)?.Value;
        // A log scope so every ILogger call in this request carries these fields (no secrets).
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = Activity.Current?.TraceId.ToString(),
            ["UserId"] = userId
        }))
        {
            await _next(context);
        }
    }

    private static string? ResolveIncoming(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var values)) return null;
        var candidate = values.ToString().Trim();
        return candidate.Length is > 0 and <= 64 && SafeId().IsMatch(candidate) ? candidate : null;
    }

    [GeneratedRegex("^[A-Za-z0-9_.:-]+$")]
    private static partial Regex SafeId();
}

/// <summary>Helper to read the current request's correlation id from anywhere with the HttpContext.</summary>
public static class CorrelationIdAccessor
{
    public static string? Get(HttpContext? context) =>
        context?.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var v) == true ? v as string : null;
}
