using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Dtos;

// Sent by the client when creating/updating an expense
public class ExpenseCreateDto
{
    [Range(0.01, 1_000_000, ErrorMessage = "Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    [MaxLength(250)]
    public string? Note { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int CategoryId { get; set; }
}

// Returned to the client (flattened, includes joined names)
public class ExpenseReadDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Note { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

// Report row: total spend grouped by category
public class CategoryTotalDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

// Report row: total spend grouped by month
public class MonthlyTotalDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Total { get; set; }
}
