namespace ExpenseTracker.Application.Contracts.Reports;

public sealed record CategoryTotalDto(int CategoryId, string CategoryName, decimal Total);

public sealed record MonthlyTotalDto(int Year, int Month, decimal Total);
