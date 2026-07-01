namespace ExpenseTracker.Application.Contracts.Categories;

public sealed record CategoryDto(int Id, string Name);

public sealed record CreateCategoryRequest(string Name);
