namespace ExpenseTracker.Domain.Services;

/// <summary>A suggested repayment: <see cref="FromUserId"/> should pay <see cref="ToUserId"/> the amount.</summary>
public sealed record DebtTransfer(string FromUserId, string ToUserId, decimal Amount);

/// <summary>
/// Turns a set of net balances into the minimum number of repayments that clears everyone out
/// (a greedy max-creditor / max-debtor matching, the same idea Splitwise uses to "simplify debts").
/// A positive net means the user is owed money; a negative net means the user owes.
/// </summary>
public static class DebtSimplifier
{
    private const decimal Epsilon = 0.005m; // treat sub-half-cent residue as settled

    public static IReadOnlyList<DebtTransfer> Simplify(IReadOnlyDictionary<string, decimal> netBalances)
    {
        // Work on mutable copies, rounded to cents.
        var creditors = netBalances
            .Where(kv => kv.Value > Epsilon)
            .Select(kv => new Holder(kv.Key, Math.Round(kv.Value, 2)))
            .ToList();
        var debtors = netBalances
            .Where(kv => kv.Value < -Epsilon)
            .Select(kv => new Holder(kv.Key, Math.Round(-kv.Value, 2)))
            .ToList();

        var transfers = new List<DebtTransfer>();

        while (creditors.Count > 0 && debtors.Count > 0)
        {
            // Always settle the largest outstanding debt against the largest credit first —
            // this keeps the number of transfers minimal in practice.
            var creditor = creditors.MaxBy(c => c.Amount)!;
            var debtor = debtors.MaxBy(d => d.Amount)!;

            var amount = Math.Min(creditor.Amount, debtor.Amount);
            transfers.Add(new DebtTransfer(debtor.UserId, creditor.UserId, amount));

            creditor.Amount -= amount;
            debtor.Amount -= amount;

            if (creditor.Amount <= Epsilon) creditors.Remove(creditor);
            if (debtor.Amount <= Epsilon) debtors.Remove(debtor);
        }

        return transfers;
    }

    private sealed class Holder(string userId, decimal amount)
    {
        public string UserId { get; } = userId;
        public decimal Amount { get; set; } = amount;
    }
}
