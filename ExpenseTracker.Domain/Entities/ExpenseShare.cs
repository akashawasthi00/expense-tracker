namespace ExpenseTracker.Domain.Entities;

/// <summary>
/// The portion of an <see cref="Expense"/> that a single participant owes.
/// </summary>
public class ExpenseShare
{
    public Guid Id { get; set; }

    public Guid ExpenseId { get; set; }
    public Expense? Expense { get; set; }

    /// <summary>Identity user id of the participant who owes this share.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>The amount this participant owes for the expense.</summary>
    public decimal Amount { get; set; }

    /// <summary>Set only for percentage splits, for transparency/auditing.</summary>
    public decimal? Percentage { get; set; }
}
