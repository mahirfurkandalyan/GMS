namespace Gms.Api.Common;

/// <summary>Thrown when an aggregate is asked to make an invalid status transition.</summary>
public sealed class InvalidStatusTransitionException : Exception
{
    public string Aggregate { get; }
    public string From { get; }
    public string To { get; }

    public InvalidStatusTransitionException(string aggregate, string from, string to)
        : base($"{aggregate} durumu '{from}' → '{to}' geçişine izin vermiyor.")
    {
        Aggregate = aggregate;
        From = from;
        To = to;
    }
}

/// <summary>
/// Small helper that lets each aggregate own its lifecycle via a static transition
/// map. Keeps status a string (DB/UI friendly) while making transitions guarded and
/// centralized per aggregate. No framework, no over-engineering.
/// </summary>
public static class StatusTransition
{
    public static bool CanTransition(IReadOnlyDictionary<string, HashSet<string>> map, string from, string to) =>
        map.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static void Ensure(IReadOnlyDictionary<string, HashSet<string>> map, string aggregate, string from, string to)
    {
        if (!CanTransition(map, from, to))
        {
            throw new InvalidStatusTransitionException(aggregate, from, to);
        }
    }
}
