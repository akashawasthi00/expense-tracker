namespace ExpenseTracker.Domain.Entities;

/// <summary>
/// A recorded repayment between two group members (FromUser pays ToUser).
/// Settlements offset the balances produced by expenses.
/// </summary>
public class Settlement
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }
    public Group? Group { get; set; }

    /// <summary>Identity user id of the payer.</summary>
    public string FromUserId { get; set; } = string.Empty;

    /// <summary>Identity user id of the payee.</summary>
    public string ToUserId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
