namespace ExpenseTracker.Domain.Entities;

/// <summary>
/// A global expense category (Food, Transport, ...). Seeded at startup and shared by all users.
/// </summary>
public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
