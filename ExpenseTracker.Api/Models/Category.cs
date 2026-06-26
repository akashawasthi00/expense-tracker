using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    // Navigation property: one category has many expenses (1-to-many)
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
