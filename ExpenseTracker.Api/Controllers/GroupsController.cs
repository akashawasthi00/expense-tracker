using ExpenseTracker.Application.Contracts.Groups;
using ExpenseTracker.Application.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Controllers;

[Route("api/groups")]
public sealed class GroupsController(IGroupService groups) : ApiControllerBase
{
    /// <summary>Creates a group; the caller becomes its first admin.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateGroupRequest request, CancellationToken ct)
    {
        var result = await groups.CreateAsync(request, ct);
        return result.IsSuccess
            ? HandleCreated(result, $"/api/groups/{result.Value!.Id}")
            : HandleResult(result);
    }

    /// <summary>Lists the groups the caller belongs to.</summary>
    [HttpGet]
    public async Task<IActionResult> MyGroups(CancellationToken ct) =>
        HandleResult(await groups.GetMyGroupsAsync(ct));

    /// <summary>Gets a single group (caller must be a member).</summary>
    [HttpGet("{groupId:guid}")]
    public async Task<IActionResult> GetById(Guid groupId, CancellationToken ct) =>
        HandleResult(await groups.GetByIdAsync(groupId, ct));

    /// <summary>Adds a member by email (admin only).</summary>
    [HttpPost("{groupId:guid}/members")]
    public async Task<IActionResult> AddMember(Guid groupId, AddMemberRequest request, CancellationToken ct) =>
        HandleResult(await groups.AddMemberAsync(groupId, request, ct));

    /// <summary>Removes a member (admin only).</summary>
    [HttpDelete("{groupId:guid}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(Guid groupId, string userId, CancellationToken ct) =>
        HandleResult(await groups.RemoveMemberAsync(groupId, userId, ct));

    /// <summary>Returns each member's net balance plus the minimal set of settlements to clear them.</summary>
    [HttpGet("{groupId:guid}/balances")]
    public async Task<IActionResult> Balances(Guid groupId, CancellationToken ct) =>
        HandleResult(await groups.GetBalancesAsync(groupId, ct));
}
