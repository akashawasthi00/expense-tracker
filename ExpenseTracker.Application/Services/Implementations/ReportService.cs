using ExpenseTracker.Application.Common.Abstractions;
using ExpenseTracker.Application.Common.Errors;
using ExpenseTracker.Application.Common.Results;
using ExpenseTracker.Application.Contracts.Reports;
using ExpenseTracker.Application.Services.Abstractions;
using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Services.Implementations;

public sealed class ReportService(IApplicationDbContext db, ICurrentUser currentUser) : IReportService
{
    public async Task<Result<IReadOnlyList<CategoryTotalDto>>> ByCategoryAsync(
        Guid? groupId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var scope = await ScopedExpensesAsync(groupId, ct);
        if (scope.IsFailure)
            return Result.Failure<IReadOnlyList<CategoryTotalDto>>(scope.Error!);

        var q = scope.Value!;
        if (from is { } f) q = q.Where(e => e.Date >= f);
        if (to is { } t) q = q.Where(e => e.Date <= t);

        var rows = await q
            .GroupBy(e => new { e.CategoryId, e.Category!.Name })
            .Select(g => new CategoryTotalDto(g.Key.CategoryId, g.Key.Name, g.Sum(e => e.Amount)))
            .OrderByDescending(r => r.Total)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<CategoryTotalDto>>(rows);
    }

    public async Task<Result<IReadOnlyList<MonthlyTotalDto>>> MonthlyAsync(
        int year, Guid? groupId, CancellationToken ct = default)
    {
        var scope = await ScopedExpensesAsync(groupId, ct);
        if (scope.IsFailure)
            return Result.Failure<IReadOnlyList<MonthlyTotalDto>>(scope.Error!);

        var rows = await scope.Value!
            .Where(e => e.Date.Year == year)
            .GroupBy(e => e.Date.Month)
            .Select(g => new MonthlyTotalDto(year, g.Key, g.Sum(e => e.Amount)))
            .OrderBy(r => r.Month)
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<MonthlyTotalDto>>(rows);
    }

    /// <summary>
    /// Returns the expense query a report should run over: a single group's expenses (membership
    /// required) or the caller's personal expenses when no group is specified.
    /// </summary>
    private async Task<Result<IQueryable<Expense>>> ScopedExpensesAsync(Guid? groupId, CancellationToken ct)
    {
        var uid = currentUser.RequireUserId();

        if (groupId is { } gid)
        {
            if (!await db.GroupMembers.AnyAsync(m => m.GroupId == gid && m.UserId == uid, ct))
                return Error.Forbidden("You are not a member of this group.");
            return Result.Success<IQueryable<Expense>>(db.Expenses.Where(e => e.GroupId == gid));
        }

        return Result.Success<IQueryable<Expense>>(
            db.Expenses.Where(e => e.GroupId == null && e.PaidByUserId == uid));
    }
}
