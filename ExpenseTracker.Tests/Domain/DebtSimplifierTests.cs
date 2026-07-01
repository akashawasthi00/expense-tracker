using ExpenseTracker.Domain.Services;

namespace ExpenseTracker.Tests.Domain;

public class DebtSimplifierTests
{
    [Fact]
    public void Single_debtor_single_creditor_produces_one_transfer()
    {
        var net = new Dictionary<string, decimal> { ["akash"] = 60m, ["riya"] = -60m };

        var transfers = DebtSimplifier.Simplify(net);

        var t = Assert.Single(transfers);
        Assert.Equal("riya", t.FromUserId);
        Assert.Equal("akash", t.ToUserId);
        Assert.Equal(60m, t.Amount);
    }

    [Fact]
    public void Two_debtors_one_creditor_clears_with_two_transfers()
    {
        // Akash is owed 6000; Riya and Sam each owe 3000 (the Goa-trip example).
        var net = new Dictionary<string, decimal> { ["akash"] = 6000m, ["riya"] = -3000m, ["sam"] = -3000m };

        var transfers = DebtSimplifier.Simplify(net);

        Assert.Equal(2, transfers.Count);
        Assert.All(transfers, t => Assert.Equal("akash", t.ToUserId));
        Assert.Equal(6000m, transfers.Sum(t => t.Amount));
    }

    [Fact]
    public void Already_settled_balances_produce_no_transfers()
    {
        var net = new Dictionary<string, decimal> { ["a"] = 0m, ["b"] = 0m };

        Assert.Empty(DebtSimplifier.Simplify(net));
    }

    [Fact]
    public void Every_transfer_amount_is_positive_and_net_is_conserved()
    {
        var net = new Dictionary<string, decimal>
        {
            ["a"] = 100m, ["b"] = -40m, ["c"] = -35m, ["d"] = -25m
        };

        var transfers = DebtSimplifier.Simplify(net);

        Assert.All(transfers, t => Assert.True(t.Amount > 0));
        Assert.Equal(100m, transfers.Sum(t => t.Amount));
    }
}
