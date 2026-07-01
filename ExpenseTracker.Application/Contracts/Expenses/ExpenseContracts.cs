using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Application.Contracts.Expenses;

/// <summary>One participant in a split. <c>Value</c> is the exact amount (Exact) or percent (Percentage); ignored for Equal.</summary>
public sealed record ExpenseParticipantInput(string UserId, decimal? Value = null);

public sealed record CreateExpenseRequest(
    int CategoryId,
    string Description,
    decimal Amount,
    DateTime? Date = null,
    Guid? GroupId = null,
    string? PaidByUserId = null,
    SplitType SplitType = SplitType.Equal,
    IReadOnlyList<ExpenseParticipantInput>? Participants = null);

public sealed record UpdateExpenseRequest(
    int CategoryId,
    string Description,
    decimal Amount,
    DateTime Date,
    SplitType SplitType = SplitType.Equal,
    IReadOnlyList<ExpenseParticipantInput>? Participants = null);

public sealed record ExpenseShareDto(string UserId, string FullName, decimal Amount, decimal? Percentage);

public sealed record ExpenseDto(
    Guid Id,
    Guid? GroupId,
    string PaidByUserId,
    string PaidByName,
    int CategoryId,
    string CategoryName,
    string Description,
    decimal Amount,
    DateTime Date,
    string SplitType,
    IReadOnlyList<ExpenseShareDto> Shares,
    DateTime CreatedAtUtc);

/// <summary>Filter + paging parameters for listing expenses.</summary>
public sealed class ExpenseQuery
{
    public Guid? GroupId { get; set; }
    public int? CategoryId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
