namespace ExpenseTracker.Domain.Enums;

/// <summary>
/// How an expense amount is divided among the participants.
/// </summary>
public enum SplitType
{
    /// <summary>Split the total equally across all participants (remainder cents distributed deterministically).</summary>
    Equal = 0,

    /// <summary>Each participant owes an explicit amount; the amounts must sum to the total.</summary>
    Exact = 1,

    /// <summary>Each participant owes a percentage; the percentages must sum to 100.</summary>
    Percentage = 2
}
