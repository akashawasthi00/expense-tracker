using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos;
using ExpenseTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly AppDbContext _db;

    // Dependency Injection: the DbContext is injected by the framework
    public ExpensesController(AppDbContext db) => _db = db;

    // GET api/expenses?categoryId=1&from=2026-01-01&to=2026-12-31
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseReadDto>>> GetAll(
        int? categoryId, DateTime? from, DateTime? to)
    {
        // Build the query lazily; EF translates this to a single SQL statement
        var query = _db.Expenses
            .Include(e => e.Category)
            .Include(e => e.User)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryId == categoryId.Value);
        if (from.HasValue)
            query = query.Where(e => e.Date >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.Date <= to.Value);

        var result = await query
            .OrderByDescending(e => e.Date)
            .Select(e => ToReadDto(e))
            .ToListAsync();

        return Ok(result);
    }

    // GET api/expenses/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExpenseReadDto>> GetById(int id)
    {
        var expense = await _db.Expenses
            .Include(e => e.Category)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (expense is null) return NotFound();
        return Ok(ToReadDto(expense));
    }

    // POST api/expenses
    [HttpPost]
    public async Task<ActionResult<ExpenseReadDto>> Create(ExpenseCreateDto dto)
    {
        // Validate referenced foreign keys exist
        if (!await _db.Users.AnyAsync(u => u.Id == dto.UserId))
            return BadRequest($"User {dto.UserId} does not exist.");
        if (!await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId))
            return BadRequest($"Category {dto.CategoryId} does not exist.");

        var expense = new Expense
        {
            Amount = dto.Amount,
            Date = dto.Date == default ? DateTime.UtcNow : dto.Date,
            Note = dto.Note,
            UserId = dto.UserId,
            CategoryId = dto.CategoryId
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        // Reload navigation props for the response
        await _db.Entry(expense).Reference(e => e.Category).LoadAsync();
        await _db.Entry(expense).Reference(e => e.User).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = expense.Id }, ToReadDto(expense));
    }

    // PUT api/expenses/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ExpenseCreateDto dto)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense is null) return NotFound();

        expense.Amount = dto.Amount;
        expense.Date = dto.Date == default ? expense.Date : dto.Date;
        expense.Note = dto.Note;
        expense.UserId = dto.UserId;
        expense.CategoryId = dto.CategoryId;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE api/expenses/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var expense = await _db.Expenses.FindAsync(id);
        if (expense is null) return NotFound();

        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static ExpenseReadDto ToReadDto(Expense e) => new()
    {
        Id = e.Id,
        Amount = e.Amount,
        Date = e.Date,
        Note = e.Note,
        UserId = e.UserId,
        UserName = e.User?.Name ?? string.Empty,
        CategoryId = e.CategoryId,
        CategoryName = e.Category?.Name ?? string.Empty
    };
}
