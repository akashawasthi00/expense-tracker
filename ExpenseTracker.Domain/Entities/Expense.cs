using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Domain.Entities;

/// <summary>
/// An expense. When <see cref="GroupId"/> is null it is a personal expense; otherwise it belongs to a
/// group and is divided among participants via <see cref="Shares"/>.
/// </summary>
public class Expense : AuditableEntity
{
    public Guid Id { get; set; }

    /// <summary>Null = personal expense; otherwise the owning group.</summary>
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }

    /// <summary>Identity user id of the member who actually paid.</summary>
    public string PaidByUserId { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public SplitType SplitType { get; set; } = SplitType.Equal;

    /// <summary>One row per participant describing what that user owes for this expense.</summary>
    public ICollection<ExpenseShare> Shares { get; set; } = new List<ExpenseShare>();
}
