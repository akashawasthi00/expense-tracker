using ExpenseTracker.Application.Common.Results;
using ExpenseTracker.Application.Contracts.Reports;

namespace ExpenseTracker.Application.Services.Abstractions;

public interface IReportService
{
    Task<Result<IReadOnlyList<CategoryTotalDto>>> ByCategoryAsync(
        Guid? groupId, DateTime? from, DateTime? to, CancellationToken ct = default);

    Task<Result<IReadOnlyList<MonthlyTotalDto>>> MonthlyAsync(
        int year, Guid? groupId, CancellationToken ct = default);
}
