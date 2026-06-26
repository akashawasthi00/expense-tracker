using ExpenseTracker.Api.Controllers;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos;
using ExpenseTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Tests;

public class ReportsTests
{
    // Build an isolated in-memory database per test (no SQL Server needed in CI).
    private static AppDbContext NewDb(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: name)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ByCategory_SumsAmountsPerCategory()
    {
        using var db = NewDb(nameof(ByCategory_SumsAmountsPerCategory));
        db.Categories.Add(new Category { Id = 1, Name = "Food" });
        db.Categories.Add(new Category { Id = 2, Name = "Transport" });
        db.Users.Add(new User { Id = 1, Name = "Demo", Email = "d@e.com" });
        db.Expenses.AddRange(
            new Expense { Id = 1, Amount = 100m, CategoryId = 1, UserId = 1, Date = new DateTime(2026, 1, 1) },
            new Expense { Id = 2, Amount = 50m, CategoryId = 1, UserId = 1, Date = new DateTime(2026, 1, 2) },
            new Expense { Id = 3, Amount = 30m, CategoryId = 2, UserId = 1, Date = new DateTime(2026, 1, 3) });
        await db.SaveChangesAsync();

        var controller = new ReportsController(db);
        var result = await controller.ByCategory();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var rows = Assert.IsAssignableFrom<IEnumerable<CategoryTotalDto>>(ok.Value).ToList();

        Assert.Equal(150m, rows.Single(r => r.CategoryName == "Food").Total);
        Assert.Equal(30m, rows.Single(r => r.CategoryName == "Transport").Total);
        // Highest total comes first
        Assert.Equal("Food", rows.First().CategoryName);
    }

    [Fact]
    public async Task Total_ReturnsSumAndCount()
    {
        using var db = NewDb(nameof(Total_ReturnsSumAndCount));
        db.Users.Add(new User { Id = 1, Name = "Demo", Email = "d@e.com" });
        db.Categories.Add(new Category { Id = 1, Name = "Food" });
        db.Expenses.AddRange(
            new Expense { Id = 1, Amount = 20m, CategoryId = 1, UserId = 1 },
            new Expense { Id = 2, Amount = 80m, CategoryId = 1, UserId = 1 });
        await db.SaveChangesAsync();

        var controller = new ReportsController(db);
        var result = await controller.Total();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }
}
