using ExpenseTracker.Application.Common.Results;
using ExpenseTracker.Application.Contracts.Groups;

namespace ExpenseTracker.Application.Services.Abstractions;

public interface IGroupService
{
    Task<Result<GroupDto>> CreateAsync(CreateGroupRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<GroupDto>>> GetMyGroupsAsync(CancellationToken ct = default);
    Task<Result<GroupDto>> GetByIdAsync(Guid groupId, CancellationToken ct = default);
    Task<Result<GroupMemberDto>> AddMemberAsync(Guid groupId, AddMemberRequest request, CancellationToken ct = default);
    Task<Result> RemoveMemberAsync(Guid groupId, string userId, CancellationToken ct = default);
    Task<Result<GroupBalancesDto>> GetBalancesAsync(Guid groupId, CancellationToken ct = default);
}
