using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Entities;

/// <summary>
/// A group of users who share expenses (e.g. "Goa Trip", "Flatmates").
/// </summary>
public class Group : AuditableEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();
}
