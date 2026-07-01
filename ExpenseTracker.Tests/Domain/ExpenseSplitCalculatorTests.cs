using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Exceptions;
using ExpenseTracker.Domain.Services;

namespace ExpenseTracker.Tests.Domain;

public class ExpenseSplitCalculatorTests
{
    private static readonly string[] Three = ["a", "b", "c"];

    [Fact]
    public void Equal_split_divides_evenly()
    {
        var shares = ExpenseSplitCalculator.Calculate(
            90m, SplitType.Equal, Three.Select(u => new SplitInput(u)).ToList());

        Assert.All(shares, s => Assert.Equal(30m, s.Amount));
        Assert.Equal(90m, shares.Sum(s => s.Amount));
    }

    [Fact]
    public void Equal_split_distributes_remainder_cents_so_total_is_exact()
    {
        // 100 / 3 = 33.333...; the calculator must not lose or invent a cent.
        var shares = ExpenseSplitCalculator.Calculate(
            100m, SplitType.Equal, Three.Select(u => new SplitInput(u)).ToList());

        Assert.Equal(100m, shares.Sum(s => s.Amount));
        Assert.Equal(33.34m, shares[0].Amount); // first participant absorbs the extra cent
        Assert.Equal(33.33m, shares[1].Amount);
        Assert.Equal(33.33m, shares[2].Amount);
    }

    [Fact]
    public void Exact_split_accepts_amounts_that_sum_to_total()
    {
        var inputs = new List<SplitInput> { new("a", 20m), new("b", 30m), new("c", 50m) };

        var shares = ExpenseSplitCalculator.Calculate(100m, SplitType.Exact, inputs);

        Assert.Equal(100m, shares.Sum(s => s.Amount));
        Assert.Equal(30m, shares.Single(s => s.UserId == "b").Amount);
    }

    [Fact]
    public void Exact_split_rejects_amounts_that_do_not_sum_to_total()
    {
        var inputs = new List<SplitInput> { new("a", 20m), new("b", 30m) };

        Assert.Throws<DomainException>(() =>
            ExpenseSplitCalculator.Calculate(100m, SplitType.Exact, inputs));
    }

    [Fact]
    public void Percentage_split_computes_amounts_and_keeps_total_exact()
    {
        var inputs = new List<SplitInput> { new("a", 33.33m), new("b", 33.33m), new("c", 33.34m) };

        var shares = ExpenseSplitCalculator.Calculate(100m, SplitType.Percentage, inputs);

        Assert.Equal(100m, shares.Sum(s => s.Amount));
        Assert.All(shares, s => Assert.NotNull(s.Percentage));
    }

    [Fact]
    public void Percentage_split_rejects_when_percentages_do_not_total_100()
    {
        var inputs = new List<SplitInput> { new("a", 50m), new("b", 40m) };

        Assert.Throws<DomainException>(() =>
            ExpenseSplitCalculator.Calculate(100m, SplitType.Percentage, inputs));
    }

    [Fact]
    public void Rejects_duplicate_participants()
    {
        var inputs = new List<SplitInput> { new("a"), new("a") };

        Assert.Throws<DomainException>(() =>
            ExpenseSplitCalculator.Calculate(50m, SplitType.Equal, inputs));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Rejects_non_positive_total(decimal total)
    {
        Assert.Throws<DomainException>(() =>
            ExpenseSplitCalculator.Calculate(total, SplitType.Equal, [new SplitInput("a")]));
    }
}
