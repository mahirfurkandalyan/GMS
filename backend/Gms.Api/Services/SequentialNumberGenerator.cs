using Microsoft.EntityFrameworkCore;

namespace Gms.Api.Services;

/// <summary>
/// Single reusable sequential-number generator (replaces the three near-identical
/// Change/Approval/Release generators). Format: {prefix}{seq:000000}, e.g.
/// CHG-2026-000001. PoC-safe: derives the next value from the max existing number.
/// Not a distributed sequence — the unique index still guards against duplicates.
/// </summary>
public class SequentialNumberGenerator
{
    /// <param name="prefix">Full prefix including year, e.g. "CHG-2026-".</param>
    /// <param name="existingNumbers">All existing numbers of that entity (e.g. c => c.ChangeNo).</param>
    public async Task<string> NextAsync(string prefix, IQueryable<string> existingNumbers, CancellationToken ct = default)
    {
        var last = await existingNumbers
            .Where(n => n.StartsWith(prefix))
            .OrderByDescending(n => n)
            .FirstOrDefaultAsync(ct);

        var next = 1;
        if (last is not null)
        {
            var numericPart = last[prefix.Length..];
            if (int.TryParse(numericPart, out var parsed))
            {
                next = parsed + 1;
            }
        }

        return $"{prefix}{next:000000}";
    }
}
