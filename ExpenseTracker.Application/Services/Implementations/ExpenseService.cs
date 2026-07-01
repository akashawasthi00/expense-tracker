using ExpenseTracker.Application.Common.Abstractions;
using ExpenseTracker.Application.Common.Errors;
using ExpenseTracker.Application.Common.Models;
using ExpenseTracker.Application.Common.Results;
using ExpenseTracker.Application.Contracts.Expenses;
using ExpenseTracker.Application.Services.Abstractions;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Exceptions;
using ExpenseTracker.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Services.Implementations;

public sealed class ExpenseService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IIdentityService identity,
    TimeProvider clock) : IExpenseService
{
    private const int MaxPageSize = 100;

    public async Task<Result<ExpenseDto>> CreateAsync(CreateExpenseRequest request, CancellationToken ct = default)
    {
        var uid = currentUser.RequireUserId();

        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct))
            return Error.Validation($"Category {request.CategoryId} does not exist.");

        var now = clock.GetUtcNow().UtcDateTime;
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Description = request.Description.Trim(),
            Amount = request.Amount,
            Date = request.Date ?? now,
            CreatedByUserId = uid,
            CreatedAtUtc = now
        };

        if (request.GroupId is null)
        {
            // Personal expense: the caller paid for themselves; no splitting.
            expense.PaidByUserId = uid;
            expense.SplitType = SplitType.Equal;
        }
        else
        {
            var build = await BuildGroupExpenseAsync(expense, request.GroupId.Value, uid,
                request.PaidByUserId, request.SplitType, request.Participants, ct);
            if (build.IsFailure)
                return Result.Failure<ExpenseDto>(build.Error!);
        }

        db.Expenses.Add(expense);
        await db.SaveChangesAsync(ct);

        return await MapAsync(expense, ct);
    }

    public async Task<Result<ExpenseDto>> GetByIdAsync(Guid expenseId, CancellationToken ct = default)
    {
        var uid = currentUser.RequireUserId();

        var expense = await db.Expenses
            .Include(e => e.Shares)
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == expenseId, ct);
        if (expense is null)
            return Error.NotFound("Expense not found.");

        if (!await CanViewAsync(expense, uid, ct))
            return Error.Forbidden("You do not have access to this expense.");

        return await MapAsync(expense, ct);
    }

    public async Task<Result<PagedResult<ExpenseDto>>> ListAsync(ExpenseQuery query, CancellationToken ct = default)
    {
        var uid = currentUser.RequireUserId();

        var myGroupIds = await db.GroupMembers
            .Where(m => m.UserId == uid)
            .Select(m => m.GroupId)
            .ToListAsync(ct);

        var q = db.Expenses
            .Include(e => e.Shares)
            .Include(e => e.Category)
            .Where(e => (e.GroupId == null && e.PaidByUserId == uid)
                        || (e.GroupId != null && myGroupIds.Contains(e.GroupId.Value)));

        if (query.GroupId is { } gid)
        {
            if (!myGroupIds.Contains(gid))
                return Error.Forbidden("You are not a member of this group.");
            q = q.Where(e => e.GroupId == gid);
        }

        if (query.CategoryId is { } cat) q = q.Where(e => e.CategoryId == cat);
        if (query.From is { } from) q = q.Where(e => e.Date >= from);
        if (query.To is { } to) q = q.Where(e => e.Date <= to);

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        var names = await ResolveNamesAsync(CollectUserIds(items), ct);
        var dtos = items.Select(e => Map(e, names)).ToList();

        return PagedResult<ExpenseDto>.Create(dtos, page, pageSize, total);
    }

    public async Task<Result<ExpenseDto>> UpdateAsync(
        Guid expenseId, UpdateExpenseRequest request, CancellationToken ct = default)
    {
        var uid = currentUser.RequireUserId();

        var expense = await db.Expenses
            .Include(e => e.Shares)
            .FirstOrDefaultAsync(e => e.Id == expenseId, ct);
        if (expense is null)
            return Error.NotFound("Expense not found.");
        if (expense.CreatedByUserId != uid)
            return Error.Forbidden("Only the person who created an expense can edit it.");

        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct))
            return Error.Validation($"Category {request.CategoryId} does not exist.");

        expense.CategoryId = request.CategoryId;
        expense.Description = request.Description.Trim();
        expense.Amount = request.Amount;
        expense.Date = request.Date;
        expense.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;

        if (expense.GroupId is { } groupId)
        {
            // Recompute the split from scratch.
            db.ExpenseShares.RemoveRange(expense.Shares);
            expense.Shares.Clear();

            var build = await BuildGroupExpenseAsync(expense, groupId, uid,
                expense.PaidByUserId, request.SplitType, request.Participants, ct);
            if (build.IsFailure)
                return Result.Failure<ExpenseDto>(build.Error!);
        }

        await db.SaveChangesAsync(ct);
        return await MapAsync(expense, ct);
    }

    public async Task<Result> DeleteAsync(Guid expenseId, CancellationToken ct = default)
    {
        var uid = currentUser.RequireUserId();

        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == expenseId, ct);
        if (expense is null)
            return Error.NotFound("Expense not found.");
        if (expense.CreatedByUserId != uid)
            return Error.Forbidden("Only the person who created an expense can delete it.");

        db.Expenses.Remove(expense); // shares cascade-delete
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ----- group-expense construction -----

    private async Task<Result> BuildGroupExpenseAsync(
        Expense expense, Guid groupId, string callerId, string? paidByUserId,
        SplitType splitType, IReadOnlyList<ExpenseParticipantInput>? participants, CancellationToken ct)
    {
        var memberIds = await db.GroupMembers
            .Where(m => m.GroupId == groupId)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        if (memberIds.Count == 0)
            return Error.NotFound("Group not found.");
        if (!memberIds.Contains(callerId))
            return Error.Forbidden("You are not a member of this group.");

        var paidBy = string.IsNullOrWhiteSpace(paidByUserId) ? callerId : paidByUserId;
        if (!memberIds.Contains(paidBy))
            return Error.Validation("The payer must be a member of the group.");

        // Default participants: for an Equal split with none supplied, everyone shares.
        var inputs = (participants is { Count: > 0 })
            ? participants.Select(p => new SplitInput(p.UserId, p.Value)).ToList()
            : splitType == SplitType.Equal
                ? memberIds.Select(id => new SplitInput(id)).ToList()
                : new List<SplitInput>();

        if (inputs.Count == 0)
            return Error.Validation("Exact and percentage splits require an explicit list of participants.");

        var unknown = inputs.Select(i => i.UserId).Except(memberIds, StringComparer.Ordinal).ToList();
        if (unknown.Count > 0)
            return Error.Validation("Every participant must be a member of the group.");

        IReadOnlyList<ShareResult> shares;
        try
        {
            shares = ExpenseSplitCalculator.Calculate(expense.Amount, splitType, inputs);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Message);
        }

        expense.GroupId = groupId;
        expense.PaidByUserId = paidBy;
        expense.SplitType = splitType;
        foreach (var s in shares)
        {
            expense.Shares.Add(new ExpenseShare
            {
                Id = Guid.NewGuid(),
                ExpenseId = expense.Id,
                UserId = s.UserId,
                Amount = s.Amount,
                Percentage = s.Percentage
            });
        }

        return Result.Success();
    }

    // ----- access + mapping -----

    private async Task<bool> CanViewAsync(Expense expense, string uid, CancellationToken ct)
    {
        if (expense.GroupId is null)
            return expense.PaidByUserId == uid || expense.CreatedByUserId == uid;

        return await db.GroupMembers.AnyAsync(m => m.GroupId == expense.GroupId && m.UserId == uid, ct);
    }

    /// <summary>Reloads the expense with its shares + category so the returned DTO is fully populated.</summary>
    private async Task<Result<ExpenseDto>> MapAsync(Expense expense, CancellationToken ct)
    {
        var loaded = await db.Expenses
            .Include(e => e.Shares)
            .Include(e => e.Category)
            .AsNoTracking()
            .FirstAsync(e => e.Id == expense.Id, ct);

        var names = await ResolveNamesAsync(CollectUserIds([loaded]), ct);
        return Map(loaded, names);
    }

    private static ExpenseDto Map(Expense e, IReadOnlyDictionary<string, AuthUser> names)
    {
        var shares = e.Shares
            .Select(s => new ExpenseShareDto(s.UserId, NameOf(names, s.UserId), s.Amount, s.Percentage))
            .ToList();

        return new ExpenseDto(
            e.Id, e.GroupId, e.PaidByUserId, NameOf(names, e.PaidByUserId),
            e.CategoryId, e.Category?.Name ?? string.Empty,
            e.Description, e.Amount, e.Date, e.SplitType.ToString(), shares, e.CreatedAtUtc);
    }

    private static IEnumerable<string> CollectUserIds(IEnumerable<Expense> expenses) =>
        expenses.SelectMany(e => e.Shares.Select(s => s.UserId).Append(e.PaidByUserId));

    private Task<IReadOnlyDictionary<string, AuthUser>> ResolveNamesAsync(
        IEnumerable<string> userIds, CancellationToken ct) =>
        identity.GetUsersAsync(userIds.Distinct(StringComparer.Ordinal), ct);

    private static string NameOf(IReadOnlyDictionary<string, AuthUser> names, string userId) =>
        names.TryGetValue(userId, out var u) ? u.FullName : "(unknown)";
}
