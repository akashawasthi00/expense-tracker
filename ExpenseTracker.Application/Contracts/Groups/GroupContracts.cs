using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Application.Contracts.Groups;

public sealed record CreateGroupRequest(string Name, string? Description);

public sealed record AddMemberRequest(string Email, GroupRole Role = GroupRole.Member);

public sealed record GroupMemberDto(string UserId, string FullName, string Email, string Role, DateTime JoinedAtUtc);

public sealed record GroupDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAtUtc,
    IReadOnlyList<GroupMemberDto> Members);

// ----- balances -----

public sealed record MemberBalanceDto(string UserId, string FullName, decimal Net);

public sealed record DebtTransferDto(
    string FromUserId, string FromName, string ToUserId, string ToName, decimal Amount);

public sealed record GroupBalancesDto(
    Guid GroupId,
    IReadOnlyList<MemberBalanceDto> Balances,
    IReadOnlyList<DebtTransferDto> SuggestedSettlements);
