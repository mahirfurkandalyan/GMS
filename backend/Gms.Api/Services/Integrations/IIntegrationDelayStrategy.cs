namespace Gms.Api.Services.Integrations;

/// <summary>
/// Computes the backoff delay before an execution's next retry. Injectable so tests can disable
/// delays (immediate) while production uses an exponential foundation. The dispatcher records the
/// suggested delay in audit; a future background worker can honour it as an eligibility window.
/// </summary>
public interface IIntegrationDelayStrategy
{
    TimeSpan NextDelay(int attemptNumber);
}

/// <summary>Exponential backoff: base * 2^(n-1), capped. (attempt 1 → base, 2 → 2×base, ...).</summary>
public sealed class ExponentialDelayStrategy : IIntegrationDelayStrategy
{
    private readonly TimeSpan _base;
    private readonly TimeSpan _max;

    public ExponentialDelayStrategy(TimeSpan? baseDelay = null, TimeSpan? maxDelay = null)
    {
        _base = baseDelay ?? TimeSpan.FromSeconds(30);
        _max = maxDelay ?? TimeSpan.FromMinutes(15);
    }

    public TimeSpan NextDelay(int attemptNumber)
    {
        if (attemptNumber <= 0) return TimeSpan.Zero;
        var factor = Math.Pow(2, Math.Min(attemptNumber - 1, 16));
        var ticks = _base.Ticks * factor;
        return ticks >= _max.Ticks ? _max : TimeSpan.FromTicks((long)ticks);
    }
}

/// <summary>No-delay strategy for tests (retries are immediately eligible).</summary>
public sealed class ImmediateDelayStrategy : IIntegrationDelayStrategy
{
    public TimeSpan NextDelay(int attemptNumber) => TimeSpan.Zero;
}
