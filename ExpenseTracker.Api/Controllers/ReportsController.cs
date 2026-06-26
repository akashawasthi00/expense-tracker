using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ReportsController(AppDbContext db) => _db = db;

    // GET api/reports/by-category
    // SQL equivalent: SELECT CategoryId, SUM(Amount) ... GROUP BY CategoryId
    [HttpGet("by-category")]
    public async Task<ActionResult<IEnumerable<CategoryTotalDto>>> ByCategory()
    {
        var data = await _db.Expenses
            .GroupBy(e => new { e.CategoryId, e.Category!.Name })
            .Select(g => new CategoryTotalDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                Total = g.Sum(e => e.Amount)
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        return Ok(data);
    }

    // GET api/reports/monthly
    // SQL equivalent: SELECT YEAR(Date), MONTH(Date), SUM(Amount) ... GROUP BY ...
    [HttpGet("monthly")]
    public async Task<ActionResult<IEnumerable<MonthlyTotalDto>>> Monthly()
    {
        var data = await _db.Expenses
            .GroupBy(e => new { e.Date.Year, e.Date.Month })
            .Select(g => new MonthlyTotalDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Total = g.Sum(e => e.Amount)
            })
            .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
            .ToListAsync();

        return Ok(data);
    }

    // GET api/reports/total
    [HttpGet("total")]
    public async Task<ActionResult<object>> Total()
    {
        var total = await _db.Expenses.SumAsync(e => (decimal?)e.Amount) ?? 0m;
        var count = await _db.Expenses.CountAsync();
        return Ok(new { totalSpend = total, expenseCount = count });
    }
}
