using ExpenseTracker.Application.Common.Models;
using ExpenseTracker.Application.Common.Results;
using ExpenseTracker.Application.Contracts.Expenses;

namespace ExpenseTracker.Application.Services.Abstractions;

public interface IExpenseService
{
    Task<Result<ExpenseDto>> CreateAsync(CreateExpenseRequest request, CancellationToken ct = default);
    Task<Result<ExpenseDto>> GetByIdAsync(Guid expenseId, CancellationToken ct = default);
    Task<Result<PagedResult<ExpenseDto>>> ListAsync(ExpenseQuery query, CancellationToken ct = default);
    Task<Result<ExpenseDto>> UpdateAsync(Guid expenseId, UpdateExpenseRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid expenseId, CancellationToken ct = default);
}
