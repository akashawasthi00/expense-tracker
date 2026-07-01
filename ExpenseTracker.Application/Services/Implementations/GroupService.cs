using ExpenseTracker.Application.Common.Abstractions;
using ExpenseTracker.Application.Common.Errors;
using ExpenseTracker.Application.Common.Results;
using ExpenseTracker.Application.Contracts.Groups;
using ExpenseTracker.Application.Services.Abstractions;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Services.Implementations;

public sealed class GroupService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IIdentityService identity,
    TimeProvider clock) : IGroupService
{
    public async Task<Result<GroupDto>> CreateAsync(CreateGroupRequest request, CancellationToken ct = default)
    {
        var uid = currentUser.RequireUserId();
        var now = clock.GetUtcNow().UtcDateTime;

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreatedByUserId = uid,
            CreatedAtUtc = now,
            Members =
            {
                new GroupMember
                {
                    Id = Guid.NewGuid(),
                    UserId = uid,
                    Role = GroupRole.Admin,   // the creator administers the group
                    JoinedAtUtc = now
                }
            }
        };

        db.Groups.Add(group);
        await db.SaveChangesAsync(ct);

        return await MapGroupAsync(group, ct);
    }

    public async Task<Result<IReadOnlyList<GroupDto>>> GetMyGroupsAsync(CancellationToken ct = default)
    {
        var uid = currentUser.RequireUserId();

        var groups = await db.Groups
            .Where(g => g.Members.Any(m => m.UserId == uid))
            .Include(g => g.Members)
            .OrderByDescending(g => g.CreatedAtUtc)
            .ToListAsync(ct);

        var names = await ResolveNamesAsync(groups.SelectMany(g => g.Members.Select(m => m.UserId)), ct);
        IReadOnlyList<GroupDto> dtos = groups.Select(g => MapGroup(g, names)).ToList();
        return Result.Success(dtos);
    }

    public async Task<Result<GroupDto>> GetByIdAsync(Guid groupId, CancellationToken ct = default)
    {
        var uid = currentUser.RequireUserId();

        var group = await db.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId, ct);

        if (group is null)
            return Error.NotFound("Group not found.");
        if (group.Members.All(m => m.UserId != uid))
            return Error.Forbidden("You are not a member of this group.");

        return await MapGroupAsync(group, ct);
    }

    public async Task<Result<GroupMemberDto>> AddMemberAsync(
        Guid groupId, AddMemberRequest request, CancellationToken ct = default)
    {
        var uid = currentUser.RequireUserId();

        var group = await db.Groups.Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group is null)
            return Error.NotFound("Group not found.");

        var caller = group.Members.FirstOrDefault(m => m.UserId == uid);
        if (caller is null)
            return Error.Forbidden("You are not a member of this group.");
        if (caller.Role != GroupRole.Admin)
            return Error.Forbidden("Only a group admin can add members.");

        var user = await identity.FindByEmailAsync(request.Email.Trim(), ct);
        if (user is null)
            return Error.NotFound($"No user found with email '{request.Email}'.");
        if (group.Members.Any(m => m.UserId == user.Id))
            return Error.Conflict("That user is already a member of the group.");

        var now = clock.GetUtcNow().UtcDateTime;
        var member = new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            UserId = user.Id,
            Role = request.Role,
            JoinedAtUtc = now
        };
        db.GroupMembers.Add(member);
        await db.SaveChangesAsync(ct);

        return new GroupMemberDto(user.Id, user.FullName, user.Email, member.Role.ToString(), member.JoinedAtUtc);
    }

    public async Task<Result> RemoveMemberAsync(Guid groupId, string userId, CancellationToken ct = default)
    {
        var uid = currentUser.RequireUserId();

        var group = await db.Groups.Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group is null)
            return Error.NotFound("Group not found.");

        var caller = group.Members.FirstOrDefault(m => m.UserId == uid);
        if (caller is null || caller.Role != GroupRole.Admin)
            return Error.Forbidden("Only a group admin can remove members.");

        var target = group.Members.FirstOrDefault(m => m.UserId == userId);
        if (target is null)
            return Error.NotFound("That user is not a member of the group.");

        if (target.Role == GroupRole.Admin && group.Members.Count(m => m.Role == GroupRole.Admin) == 1)
            return Error.Conflict("A group must keep at least one admin.");

        var hasActivity = await db.Expenses.AnyAsync(e => e.GroupId == groupId && e.PaidByUserId == userId, ct)
            || await db.ExpenseShares.AnyAsync(s => s.UserId == userId && s.Expense!.GroupId == groupId, ct);
        if (hasActivity)
            return Error.Conflict("This member has expenses in the group and cannot be removed.");

        db.GroupMembers.Remove(target);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<GroupBalancesDto>> GetBalancesAsync(Guid groupId, CancellationToken ct = default)
    {
        var uid = currentUser.RequireUserId();

        var memberIds = await db.GroupMembers
            .Where(m => m.GroupId == groupId)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        if (memberIds.Count == 0)
            return Error.NotFound("Group not found.");
        if (!memberIds.Contains(uid))
            return Error.Forbidden("You are not a member of this group.");

        // net = paid - owed + settlementsPaid - settlementsReceived  (per member)
        var net = memberIds.ToDictionary(id => id, _ => 0m, StringComparer.Ordinal);

        var paid = await db.Expenses
            .Where(e => e.GroupId == groupId)
            .GroupBy(e => e.PaidByUserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(e => e.Amount) })
            .ToListAsync(ct);
        foreach (var p in paid) Add(net, p.UserId, p.Total);

        var owed = await db.ExpenseShares
            .Where(s => s.Expense!.GroupId == groupId)
            .GroupBy(s => s.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(s => s.Amount) })
            .ToListAsync(ct);
        foreach (var o in owed) Add(net, o.UserId, -o.Total);

        var settlements = await db.Settlements.Where(s => s.GroupId == groupId).ToListAsync(ct);
        foreach (var s in settlements)
        {
            Add(net, s.FromUserId, s.Amount);
            Add(net, s.ToUserId, -s.Amount);
        }

        var names = await ResolveNamesAsync(memberIds, ct);

        var balances = net
            .OrderByDescending(kv => kv.Value)
            .Select(kv => new MemberBalanceDto(kv.Key, NameOf(names, kv.Key), Math.Round(kv.Value, 2)))
            .ToList();

        var transfers = DebtSimplifier.Simplify(net)
            .Select(t => new DebtTransferDto(
                t.FromUserId, NameOf(names, t.FromUserId),
                t.ToUserId, NameOf(names, t.ToUserId), t.Amount))
            .ToList();

        return new GroupBalancesDto(groupId, balances, transfers);
    }

    // ----- helpers -----

    private static void Add(Dictionary<string, decimal> map, string userId, decimal delta)
    {
        // Ignore activity from users who are no longer members (defensive; shouldn't happen).
        if (map.ContainsKey(userId)) map[userId] += delta;
    }

    private async Task<Result<GroupDto>> MapGroupAsync(Group group, CancellationToken ct)
    {
        var names = await ResolveNamesAsync(group.Members.Select(m => m.UserId), ct);
        return MapGroup(group, names);
    }

    private static GroupDto MapGroup(Group group, IReadOnlyDictionary<string, AuthUser> names)
    {
        var members = group.Members
            .OrderBy(m => m.JoinedAtUtc)
            .Select(m => new GroupMemberDto(
                m.UserId,
                names.TryGetValue(m.UserId, out var u) ? u.FullName : "(unknown)",
                names.TryGetValue(m.UserId, out var e) ? e.Email : string.Empty,
                m.Role.ToString(),
                m.JoinedAtUtc))
            .ToList();

        return new GroupDto(group.Id, group.Name, group.Description, group.CreatedAtUtc, members);
    }

    private Task<IReadOnlyDictionary<string, AuthUser>> ResolveNamesAsync(
        IEnumerable<string> userIds, CancellationToken ct) =>
        identity.GetUsersAsync(userIds.Distinct(StringComparer.Ordinal), ct);

    private static string NameOf(IReadOnlyDictionary<string, AuthUser> names, string userId) =>
        names.TryGetValue(userId, out var u) ? u.FullName : "(unknown)";
}
