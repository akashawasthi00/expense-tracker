using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Navigation property: one user has many expenses (1-to-many)
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
