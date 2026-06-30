using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]

//Testing for Ci/Cd 
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) => _db = db;

    // GET api/categories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetAll() =>
        Ok(await _db.Categories.OrderBy(c => c.Name).ToListAsync());

    // POST api/categories
    [HttpPost]
    public async Task<ActionResult<Category>> Create(Category category)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = category.Id }, category);
    }

    // DELETE api/categories/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        // Block delete if expenses reference it (mirrors the FK Restrict rule)
        if (await _db.Expenses.AnyAsync(e => e.CategoryId == id))
            return BadRequest("Cannot delete a category that has expenses.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
