using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Exceptions;

namespace ExpenseTracker.Domain.Services;

/// <summary>One participant's input to a split. <see cref="Value"/> is the amount (Exact) or percent (Percentage).</summary>
public readonly record struct SplitInput(string UserId, decimal? Value = null);

/// <summary>The computed share a participant owes.</summary>
public sealed record ShareResult(string UserId, decimal Amount, decimal? Percentage);

/// <summary>
/// Pure, deterministic logic that divides an expense total among participants according to the
/// chosen <see cref="SplitType"/>. All currency math is done in integer cents so the resulting
/// shares always sum <b>exactly</b> to the total (no lost or invented cents from rounding).
/// </summary>
public static class ExpenseSplitCalculator
{
    public static IReadOnlyList<ShareResult> Calculate(
        decimal total, SplitType type, IReadOnlyList<SplitInput> inputs)
    {
        if (total <= 0)
            throw new DomainException("Expense amount must be greater than zero.");
        if (inputs is null || inputs.Count == 0)
            throw new DomainException("An expense must have at least one participant.");
        if (inputs.Select(i => i.UserId).Distinct(StringComparer.Ordinal).Count() != inputs.Count)
            throw new DomainException("A participant appears more than once in the split.");

        return type switch
        {
            SplitType.Equal => SplitEqually(total, inputs),
            SplitType.Exact => SplitExactly(total, inputs),
            SplitType.Percentage => SplitByPercentage(total, inputs),
            _ => throw new DomainException($"Unsupported split type '{type}'.")
        };
    }

    private static IReadOnlyList<ShareResult> SplitEqually(decimal total, IReadOnlyList<SplitInput> inputs)
    {
        var totalCents = ToCents(total);
        var n = inputs.Count;
        var baseCents = totalCents / n;
        var remainder = (int)(totalCents % n); // first `remainder` participants get one extra cent

        var results = new List<ShareResult>(n);
        for (var i = 0; i < n; i++)
        {
            var cents = baseCents + (i < remainder ? 1 : 0);
            results.Add(new ShareResult(inputs[i].UserId, FromCents(cents), Percentage: null));
        }
        return results;
    }

    private static IReadOnlyList<ShareResult> SplitExactly(decimal total, IReadOnlyList<SplitInput> inputs)
    {
        if (inputs.Any(i => i.Value is null or < 0))
            throw new DomainException("Every participant must have a non-negative exact amount.");

        var sum = inputs.Sum(i => i.Value!.Value);
        if (ToCents(sum) != ToCents(total))
            throw new DomainException(
                $"Exact split amounts ({sum:0.00}) must add up to the expense total ({total:0.00}).");

        return inputs
            .Select(i => new ShareResult(i.UserId, Round(i.Value!.Value), Percentage: null))
            .ToList();
    }

    private static IReadOnlyList<ShareResult> SplitByPercentage(decimal total, IReadOnlyList<SplitInput> inputs)
    {
        if (inputs.Any(i => i.Value is null or < 0))
            throw new DomainException("Every participant must have a non-negative percentage.");

        var sumPct = inputs.Sum(i => i.Value!.Value);
        if (Math.Round(sumPct, 4) != 100m)
            throw new DomainException($"Split percentages must add up to 100 (got {sumPct:0.##}).");

        // Largest-remainder method: floor each share to a cent, then hand out the leftover
        // cents to the participants with the biggest fractional parts so the sum stays exact.
        var totalCents = ToCents(total);
        var raw = inputs
            .Select((i, idx) =>
            {
                var exact = totalCents * i.Value!.Value / 100m;
                var floor = (long)Math.Floor(exact);
                return (idx, floor, frac: exact - floor);
            })
            .ToList();

        var distributed = raw.Sum(r => r.floor);
        var leftover = (int)(totalCents - distributed);

        var bonus = raw
            .OrderByDescending(r => r.frac)
            .ThenBy(r => r.idx)
            .Take(leftover)
            .Select(r => r.idx)
            .ToHashSet();

        return inputs
            .Select((input, idx) =>
            {
                var cents = raw[idx].floor + (bonus.Contains(idx) ? 1 : 0);
                return new ShareResult(input.UserId, FromCents(cents), input.Value);
            })
            .ToList();
    }

    private static long ToCents(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
    private static decimal FromCents(long cents) => cents / 100m;
    private static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
