using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExpenseTracker.Api.Models;

public class Expense
{
    public int Id { get; set; }

    [Range(0.01, 1_000_000)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;

    [MaxLength(250)]
    public string? Note { get; set; }

    // Foreign key + navigation to User
    public int UserId { get; set; }
    public User? User { get; set; }

    // Foreign key + navigation to Category
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
}
