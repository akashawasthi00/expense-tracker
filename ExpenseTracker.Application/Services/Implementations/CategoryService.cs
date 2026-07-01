using ExpenseTracker.Application.Common.Abstractions;
using ExpenseTracker.Application.Common.Errors;
using ExpenseTracker.Application.Common.Results;
using ExpenseTracker.Application.Contracts.Categories;
using ExpenseTracker.Application.Services.Abstractions;
using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Services.Implementations;

public sealed class CategoryService(IApplicationDbContext db) : ICategoryService
{
    public async Task<Result<IReadOnlyList<CategoryDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var categories = await db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name))
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<CategoryDto>>(categories);
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (name.Length == 0)
            return Error.Validation("Category name is required.");

        var exists = await db.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower(), ct);
        if (exists)
            return Error.Conflict($"A category named '{name}' already exists.");

        var category = new Category { Name = name };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return new CategoryDto(category.Id, category.Name);
    }
}
