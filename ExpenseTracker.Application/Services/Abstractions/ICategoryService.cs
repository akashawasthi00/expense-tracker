using ExpenseTracker.Application.Common.Results;
using ExpenseTracker.Application.Contracts.Categories;

namespace ExpenseTracker.Application.Services.Abstractions;

public interface ICategoryService
{
    Task<Result<IReadOnlyList<CategoryDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default);
}
